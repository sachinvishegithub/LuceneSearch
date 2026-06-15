using LuceneSearchHelper.Mapping;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LuceneSearchHelper.Interceptors;

public sealed class LuceneSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<LuceneSaveChangesInterceptor> _logger;
    private readonly AsyncLocal<List<PendingSync>?> _pending = new();

    private readonly IEntityDocumentMapper _documentMapper;

    public LuceneSaveChangesInterceptor(
        IServiceScopeFactory scopeFactory,
        IEntityDocumentMapper documentMapper,
        ILogger<LuceneSaveChangesInterceptor> logger)
    {
        _scopeFactory = scopeFactory;
        _documentMapper = documentMapper;
        _logger = logger;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        CapturePending(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        CapturePending(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        SyncPendingAsync(eventData.Context, CancellationToken.None).GetAwaiter().GetResult();
        return base.SavedChanges(eventData, result);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        await SyncPendingAsync(eventData.Context, cancellationToken);
        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    private void CapturePending(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var entries = context.ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Select(e => new PendingSync(e.Entity, e.State))
            .Where(p => _documentMapper.IsIndexedEntity(p.Entity))
            .ToList();

        _pending.Value = entries;
    }

    private async Task SyncPendingAsync(DbContext? context, CancellationToken cancellationToken)
    {
        var pending = _pending.Value;
        _pending.Value = null;

        if (pending is null || pending.Count == 0 || context is null)
        {
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var indexing = scope.ServiceProvider.GetRequiredService<Services.ILuceneIndexingService>();

        foreach (var item in pending)
        {
            try
            {
                if (item.State == EntityState.Deleted)
                {
                    await indexing.DeleteEntityAsync(item.Entity, cancellationToken);
                }
                else
                {
                    await indexing.IndexEntityAsync(item.Entity, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to sync entity {EntityType} to Lucene.", item.Entity.GetType().Name);
            }
        }
    }

    private sealed record PendingSync(object Entity, EntityState State);
}

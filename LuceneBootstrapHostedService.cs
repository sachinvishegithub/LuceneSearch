using LuceneSearchHelper.Configuration;
using LuceneSearchHelper.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LuceneSearchHelper;

public sealed class LuceneBootstrapHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<LuceneBootstrapHostedService> _logger;
    private readonly LuceneOptions _options;

    public LuceneBootstrapHostedService(
        IServiceProvider serviceProvider,
        IOptions<LuceneOptions> options,
        ILogger<LuceneBootstrapHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options.Value;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Lucene integration disabled via configuration.");
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var indexManager = scope.ServiceProvider.GetRequiredService<ILuceneIndexManager>();
        await indexManager.EnsureIndexAsync(cancellationToken);

        if (_options.ReindexOnStartup)
        {
            var indexer = scope.ServiceProvider.GetService<IDatabaseLusceneIndexer>();
            if (indexer is null)
            {
                _logger.LogWarning(
                    "Lucene ReindexOnStartup is enabled but no {Indexer} was registered.",
                    nameof(IDatabaseLusceneIndexer));
            }
            else
            {
                _logger.LogWarning("Lucene ReindexOnStartup is enabled; performing full database reindex.");
                await indexer.ReindexAllAsync(cancellationToken);
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        if (_serviceProvider.GetService<ILuceneIndexProvider>() is IDisposable disposable)
        {
            disposable.Dispose();
        }

        return Task.CompletedTask;
    }
}

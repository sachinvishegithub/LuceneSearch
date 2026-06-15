using Microsoft.Extensions.Logging;

namespace LuceneSearchHelper.Services;

public interface ILuceneIndexManager
{
    Task EnsureIndexAsync(CancellationToken cancellationToken = default);
}

public sealed class LuceneIndexManager : ILuceneIndexManager
{
    private readonly ILuceneIndexProvider _indexProvider;
    private readonly ILogger<LuceneIndexManager> _logger;

    public LuceneIndexManager(ILuceneIndexProvider indexProvider, ILogger<LuceneIndexManager> logger)
    {
        _indexProvider = indexProvider;
        _logger = logger;
    }

    public Task EnsureIndexAsync(CancellationToken cancellationToken = default)
    {
        if (!_indexProvider.Options.Enabled)
        {
            _logger.LogInformation("Lucene disabled; skipping index ensure.");
            return Task.CompletedTask;
        }

        var path = Path.GetFullPath(_indexProvider.Options.IndexDirectoryPath);
        System.IO.Directory.CreateDirectory(path);
        _logger.LogDebug("Lucene index directory ready at {Path}.", path);
        return Task.CompletedTask;
    }
}

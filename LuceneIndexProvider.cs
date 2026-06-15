using LuceneSearchHelper.Configuration;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LuceneSearchHelper;

public interface ILuceneIndexProvider : IDisposable
{
    LuceneOptions Options { get; }
    IndexWriter Writer { get; }
    SearcherManager SearcherManager { get; }
    void RefreshSearcher();
}

public sealed class LuceneIndexProvider : ILuceneIndexProvider
{
    private readonly FSDirectory _directory;
    private readonly IndexWriter _writer;
    private readonly SearcherManager _searcherManager;

    public LuceneIndexProvider(IOptions<LuceneOptions> options, ILogger<LuceneIndexProvider> logger)
    {
        Options = options.Value;
        var indexPath = Path.GetFullPath(Options.IndexDirectoryPath);
        System.IO.Directory.CreateDirectory(indexPath);

        _directory = FSDirectory.Open(indexPath);
        var analyzer = new StandardAnalyzer(LuceneVersion.LUCENE_48);
        var writerConfig = new IndexWriterConfig(LuceneVersion.LUCENE_48, analyzer)
        {
            OpenMode = OpenMode.CREATE_OR_APPEND,
        };

        _writer = new IndexWriter(_directory, writerConfig);
        _searcherManager = new SearcherManager(_writer, applyAllDeletes: true, searcherFactory: null);

        logger.LogInformation("Lucene index configured at {IndexPath}", indexPath);
    }

    public LuceneOptions Options { get; }

    public IndexWriter Writer => _writer;

    public SearcherManager SearcherManager => _searcherManager;

    public void RefreshSearcher()
    {
        _writer.Commit();
        _searcherManager.MaybeRefresh();
    }

    public void Dispose()
    {
        _searcherManager.Dispose();
        _writer.Dispose();
        _directory.Dispose();
    }
}

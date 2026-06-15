using LuceneSearchHelper.Mapping;
using LuceneSearchHelper.Models;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Microsoft.Extensions.Logging;

namespace LuceneSearchHelper.Services;

public interface ILuceneSearchService
{
    Task<IReadOnlyList<GlobalSearchResult>> SearchAsync(string searchTerm, int? size = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> SuggestAsync(string prefix, int? size = null, CancellationToken cancellationToken = default);
}

public sealed class LuceneSearchService : ILuceneSearchService
{
    private readonly ILuceneIndexProvider _indexProvider;
    private readonly LuceneQueryBuilder _queryBuilder;
    private readonly ILogger<LuceneSearchService> _logger;

    public LuceneSearchService(
        ILuceneIndexProvider indexProvider,
        LuceneQueryBuilder queryBuilder,
        ILogger<LuceneSearchService> logger)
    {
        _indexProvider = indexProvider;
        _queryBuilder = queryBuilder;
        _logger = logger;
    }

    public Task<IReadOnlyList<GlobalSearchResult>> SearchAsync(
        string searchTerm,
        int? size = null,
        CancellationToken cancellationToken = default)
    {
        if (!_indexProvider.Options.Enabled || string.IsNullOrWhiteSpace(searchTerm))
        {
            return Task.FromResult<IReadOnlyList<GlobalSearchResult>>(Array.Empty<GlobalSearchResult>());
        }

        var take = size ?? _indexProvider.Options.DefaultSearchSize;

        try
        {
            var searcher = _indexProvider.SearcherManager.Acquire();

            try
            {
                var query = _queryBuilder.Build(searchTerm, _indexProvider.Writer.Analyzer);
                var topDocs = searcher.Search(query, take);
                var results = new List<GlobalSearchResult>(topDocs.ScoreDocs.Length);

                foreach (var scoreDoc in topDocs.ScoreDocs)
                {
                    var doc = searcher.Doc(scoreDoc.Doc);
                    var searchText = doc.Get(LuceneDocumentFactory.SearchTextField);
                    results.Add(new GlobalSearchResult
                    {
                        DocumentId = doc.Get(LuceneDocumentFactory.DocumentIdField) ?? string.Empty,
                        EntityType = doc.Get(LuceneDocumentFactory.EntityTypeField) ?? string.Empty,
                        EntityId = doc.Get(LuceneDocumentFactory.EntityIdField) ?? string.Empty,
                        Title = doc.Get(LuceneDocumentFactory.TitleField) ?? string.Empty,
                        Subtitle = NullIfEmpty(doc.Get(LuceneDocumentFactory.SubtitleField)),
                        Snippet = BuildSnippet(searchText, searchTerm),
                        Route = NullIfEmpty(doc.Get(LuceneDocumentFactory.RouteField)),
                        Score = scoreDoc.Score,
                    });
                }

                return Task.FromResult<IReadOnlyList<GlobalSearchResult>>(results);
            }
            finally
            {
                _indexProvider.SearcherManager.Release(searcher);
            }
        }
        catch (ParseException ex)
        {
            _logger.LogWarning(ex, "Lucene query parse failed for term '{SearchTerm}'", searchTerm);
            return Task.FromResult<IReadOnlyList<GlobalSearchResult>>(Array.Empty<GlobalSearchResult>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lucene search exception for term '{SearchTerm}'", searchTerm);
            return Task.FromResult<IReadOnlyList<GlobalSearchResult>>(Array.Empty<GlobalSearchResult>());
        }
    }

    public async Task<IReadOnlyList<string>> SuggestAsync(
        string prefix,
        int? size = null,
        CancellationToken cancellationToken = default)
    {
        var results = await SearchAsync(prefix, size ?? _indexProvider.Options.SuggestSize, cancellationToken);
        return results
            .Select(r => r.Title)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(size ?? _indexProvider.Options.SuggestSize)
            .ToList();
    }

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;

    private static string? BuildSnippet(string? searchText, string term)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return null;
        }

        var index = searchText.IndexOf(term, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
        {
            return searchText.Length <= 160 ? searchText : searchText[..160] + "…";
        }

        var start = Math.Max(0, index - 40);
        var length = Math.Min(searchText.Length - start, 160);
        var snippet = searchText.Substring(start, length);
        return start > 0 ? "…" + snippet : snippet;
    }
}

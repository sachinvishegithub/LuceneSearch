using LuceneSearchHelper.Documents;
using LuceneSearchHelper.Mapping;
using Microsoft.Extensions.Logging;

namespace LuceneSearchHelper.Services;

public interface ILuceneIndexingService
{
    Task IndexDocumentAsync(SearchDocument document, CancellationToken cancellationToken = default);
    Task DeleteDocumentAsync(string documentId, CancellationToken cancellationToken = default);
    Task IndexEntityAsync(object entity, CancellationToken cancellationToken = default);
    Task DeleteEntityAsync(object entity, CancellationToken cancellationToken = default);
}

public sealed class LuceneIndexingService : ILuceneIndexingService
{
    private readonly ILuceneIndexProvider _indexProvider;
    private readonly IEntityDocumentMapper _documentMapper;
    private readonly ILogger<LuceneIndexingService> _logger;

    public LuceneIndexingService(
        ILuceneIndexProvider indexProvider,
        IEntityDocumentMapper documentMapper,
        ILogger<LuceneIndexingService> logger)
    {
        _indexProvider = indexProvider;
        _documentMapper = documentMapper;
        _logger = logger;
    }

    public Task IndexDocumentAsync(SearchDocument document, CancellationToken cancellationToken = default)
    {
        if (!_indexProvider.Options.Enabled)
        {
            return Task.CompletedTask;
        }

        try
        {
            var luceneDoc = LuceneDocumentFactory.ToLuceneDocument(document);
            _indexProvider.Writer.UpdateDocument(LuceneDocumentFactory.DocumentIdTerm(document.DocumentId), luceneDoc);
            _indexProvider.RefreshSearcher();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lucene index exception for document {DocumentId}", document.DocumentId);
        }

        return Task.CompletedTask;
    }

    public Task DeleteDocumentAsync(string documentId, CancellationToken cancellationToken = default)
    {
        if (!_indexProvider.Options.Enabled)
        {
            return Task.CompletedTask;
        }

        try
        {
            _indexProvider.Writer.DeleteDocuments(LuceneDocumentFactory.DocumentIdTerm(documentId));
            _indexProvider.RefreshSearcher();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lucene delete exception for document {DocumentId}", documentId);
        }

        return Task.CompletedTask;
    }

    public async Task IndexEntityAsync(object entity, CancellationToken cancellationToken = default)
    {
        var document = _documentMapper.Map(entity);
        if (document is null)
        {
            return;
        }

        await IndexDocumentAsync(document, cancellationToken);
    }

    public async Task DeleteEntityAsync(object entity, CancellationToken cancellationToken = default)
    {
        var documentId = _documentMapper.GetDocumentId(entity);
        if (string.IsNullOrWhiteSpace(documentId))
        {
            _logger.LogWarning("Cannot delete Lucene document; missing identity for {EntityType}", entity.GetType().Name);
            return;
        }

        await DeleteDocumentAsync(documentId, cancellationToken);
    }
}

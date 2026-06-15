namespace LuceneSearchHelper.Documents;

/// <summary>
/// Unified Lucene document for any indexed entity.
/// documentId format: {entityType}_{primaryKey}
/// </summary>
public sealed class SearchDocument
{
    public required string DocumentId { get; set; }

    public required string EntityType { get; set; }

    public required string EntityId { get; set; }

    /// <summary>Short label shown in search UI.</summary>
    public required string Title { get; set; }

    /// <summary>Concatenated searchable text from mapped SQL columns.</summary>
    public required string SearchText { get; set; }

    public string? Subtitle { get; set; }

    /// <summary>Deep link path in the Angular app when applicable.</summary>
    public string? Route { get; set; }

    public DateTime? UpdatedOn { get; set; }
}

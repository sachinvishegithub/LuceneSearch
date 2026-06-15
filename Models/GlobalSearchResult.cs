namespace LuceneSearchHelper.Models;

public sealed class GlobalSearchResult
{
    public required string DocumentId { get; set; }
    public required string EntityType { get; set; }
    public required string EntityId { get; set; }
    public required string Title { get; set; }
    public string? Subtitle { get; set; }
    public string? Snippet { get; set; }
    public string? Route { get; set; }
    public double? Score { get; set; }
}

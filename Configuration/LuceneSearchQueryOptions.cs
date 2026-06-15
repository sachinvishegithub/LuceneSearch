namespace LuceneSearchHelper.Configuration;

public sealed class LuceneSearchQueryOptions
{
    public Dictionary<string, float> FieldBoosts { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["title"] = 3f,
        ["subtitle"] = 2f,
        ["searchText"] = 1f,
    };

    public bool EnableFuzzySearch { get; set; } = true;

    /// <summary>Lucene fuzzy minimum similarity (0–1). Maps to ~suffix on parsed terms.</summary>
    public float FuzzyMinSimilarity { get; set; } = 0.8f;

    public bool EnableProximitySearch { get; set; } = true;

    /// <summary>Default slop for multi-term proximity (phrase~slop).</summary>
    public int ProximitySlop { get; set; } = 4;
}

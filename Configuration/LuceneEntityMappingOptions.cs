namespace LuceneSearchHelper.Configuration;

public sealed class LuceneEntityMappingOptions
{
    public const string SectionName = "Lucene:EntityMapping";

    /// <summary>Route templates by mapping mode when an entity has no <see cref="LuceneEntityTypeDefinition.RouteTemplate"/>.</summary>
    public Dictionary<string, string> RouteTemplates { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        [nameof(LuceneMappingMode.Simple)] = "/admin",
        [nameof(LuceneMappingMode.Entity)] = "/dashboard/{Id}",
        [nameof(LuceneMappingMode.DomainObject)] = "/dashboard/{Id}",
    };

    public List<LuceneEntityTypeDefinition> Entities { get; set; } = [];
}

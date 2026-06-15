namespace LuceneSearchHelper.Configuration;

public sealed class LuceneEntityTypeDefinition
{
    /// <summary>Matches <see cref="Type.Name"/> (e.g. Hit, Note).</summary>
    public required string TypeName { get; set; }

    /// <summary>Optional display/index entity type; defaults to <see cref="TypeName"/>.</summary>
    public string? EntityTypeAlias { get; set; }

    public LuceneMappingMode MappingMode { get; set; } = LuceneMappingMode.Simple;

    /// <summary>Overrides route template for this type. Placeholders: {PropertyName}.</summary>
    public string? RouteTemplate { get; set; }

    public string? TitleProperty { get; set; }

    public string? SubtitleProperty { get; set; }

    /// <summary>Used when the title property is null or whitespace (e.g. Incident #{Id}).</summary>
    public string? TitleWhenEmpty { get; set; }

    public string? UpdatedOnProperty { get; set; }

    /// <summary>Defaults to Id. Use multiple names for composite keys (joined with _).</summary>
    public string[] PrimaryKeyProperties { get; set; } = ["Id"];

    /// <summary>Navigation graph depth for Entity / DomainObject modes.</summary>
    public int MaxNavigationDepth { get; set; }

    public string[] ExcludeProperties { get; set; } = [];

    public int TitleMaxLength { get; set; } = 120;
}

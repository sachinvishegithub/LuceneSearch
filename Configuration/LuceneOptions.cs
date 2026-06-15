namespace LuceneSearchHelper.Configuration;

public class LuceneOptions
{
    public const string SectionName = "Lucene";

    public bool Enabled { get; set; } = true;

    /// <summary>Filesystem directory for the Lucene index (created if missing).</summary>
    public string IndexDirectoryPath { get; set; } = "lucene-index";

    public bool ReindexOnStartup { get; set; }

    public int DefaultSearchSize { get; set; } = 25;

    public int SuggestSize { get; set; } = 10;

    public LuceneSearchQueryOptions Search { get; set; } = new();
}

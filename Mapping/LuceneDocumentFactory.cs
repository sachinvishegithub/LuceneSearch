using LuceneSearchHelper.Documents;
using Lucene.Net.Documents;
using Lucene.Net.Index;

namespace LuceneSearchHelper.Mapping;

internal static class LuceneDocumentFactory
{
    public const string DocumentIdField = "documentId";
    public const string EntityTypeField = "entityType";
    public const string EntityIdField = "entityId";
    public const string TitleField = "title";
    public const string SubtitleField = "subtitle";
    public const string SearchTextField = "searchText";
    public const string RouteField = "route";
    public const string UpdatedOnField = "updatedOn";

    public static Document ToLuceneDocument(SearchDocument source)
    {
        var doc = new Document
        {
            new StringField(DocumentIdField, source.DocumentId, Field.Store.YES),
            new StringField(EntityTypeField, source.EntityType, Field.Store.YES),
            new StringField(EntityIdField, source.EntityId, Field.Store.YES),
            new TextField(TitleField, source.Title ?? string.Empty, Field.Store.YES),
            new TextField(SubtitleField, source.Subtitle ?? string.Empty, Field.Store.YES),
            new TextField(SearchTextField, source.SearchText ?? string.Empty, Field.Store.YES),
            new StringField(RouteField, source.Route ?? string.Empty, Field.Store.YES),
            new StringField(
                UpdatedOnField,
                source.UpdatedOn?.ToUniversalTime().Ticks.ToString() ?? string.Empty,
                Field.Store.YES),
        };

        return doc;
    }

    public static Term DocumentIdTerm(string documentId) => new(DocumentIdField, documentId);
}

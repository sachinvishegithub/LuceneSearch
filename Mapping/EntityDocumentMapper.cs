using LuceneSearchHelper.Configuration;
using LuceneSearchHelper.Documents;
using Microsoft.Extensions.Options;

namespace LuceneSearchHelper.Mapping;

public interface IEntityDocumentMapper
{
    bool IsIndexedEntity(object entity);

    SearchDocument? Map(object entity);

    string BuildDocumentId(string entityType, string entityId);

    string? GetPrimaryKey(object entity);

    string? GetDocumentId(object entity);
}

public sealed class EntityDocumentMapper : IEntityDocumentMapper
{
    private readonly IOptionsMonitor<LuceneEntityMappingOptions> _mappingOptions;

    public EntityDocumentMapper(IOptionsMonitor<LuceneEntityMappingOptions> mappingOptions)
    {
        _mappingOptions = mappingOptions;
    }

    public bool IsIndexedEntity(object entity)
    {
        return TryGetDefinition(entity.GetType(), out _);
    }

    public SearchDocument? Map(object entity)
    {
        if (!TryGetDefinition(entity.GetType(), out var definition) || definition is null)
        {
            return null;
        }

        var entityId = ReflectionMappingHelper.GetPrimaryKey(entity, definition);
        if (string.IsNullOrWhiteSpace(entityId))
        {
            return null;
        }

        var entityType = definition.EntityTypeAlias ?? definition.TypeName;
        var options = _mappingOptions.CurrentValue;
        var routeTemplate = definition.RouteTemplate
            ?? options.RouteTemplates.GetValueOrDefault(definition.MappingMode.ToString());

        var maxDepth = ReflectionMappingHelper.ResolveMaxDepth(definition);
        var searchText = ReflectionMappingHelper.CollectSearchText(entity, definition, maxDepth);

        return new SearchDocument
        {
            DocumentId = BuildDocumentId(entityType, entityId),
            EntityType = entityType,
            EntityId = entityId,
            Title = ReflectionMappingHelper.ResolveTitle(entity, definition),
            Subtitle = ReflectionMappingHelper.ResolveSubtitle(entity, definition),
            SearchText = string.IsNullOrWhiteSpace(searchText)
                ? ReflectionMappingHelper.ResolveTitle(entity, definition)
                : searchText,
            Route = RouteTemplateResolver.Resolve(routeTemplate, entity),
            UpdatedOn = ReflectionMappingHelper.ResolveUpdatedOn(entity, definition),
        };
    }

    public string BuildDocumentId(string entityType, string entityId) => $"{entityType}_{entityId}";

    public string? GetPrimaryKey(object entity)
    {
        if (!TryGetDefinition(entity.GetType(), out var definition) || definition is null)
        {
            return null;
        }

        return ReflectionMappingHelper.GetPrimaryKey(entity, definition);
    }

    public string? GetDocumentId(object entity)
    {
        if (!TryGetDefinition(entity.GetType(), out var definition) || definition is null)
        {
            return null;
        }

        var entityId = ReflectionMappingHelper.GetPrimaryKey(entity, definition);
        if (string.IsNullOrWhiteSpace(entityId))
        {
            return null;
        }

        var entityType = definition.EntityTypeAlias ?? definition.TypeName;
        return BuildDocumentId(entityType, entityId);
    }

    private bool TryGetDefinition(Type type, out LuceneEntityTypeDefinition? definition)
    {
        var options = _mappingOptions.CurrentValue;
        definition = options.Entities.FirstOrDefault(e =>
            string.Equals(e.TypeName, type.Name, StringComparison.Ordinal));

        return definition is not null;
    }
}

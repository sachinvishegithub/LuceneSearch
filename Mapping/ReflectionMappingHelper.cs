using System.Collections;
using System.Reflection;
using System.Text;
using System.Text.Json;
using LuceneSearchHelper.Configuration;

namespace LuceneSearchHelper.Mapping;

internal static class ReflectionMappingHelper
{
    private static readonly HashSet<Type> ScalarTypes =
    [
        typeof(string),
        typeof(int), typeof(int?),
        typeof(long), typeof(long?),
        typeof(decimal), typeof(decimal?),
        typeof(bool), typeof(bool?),
        typeof(DateTime), typeof(DateTime?),
        typeof(double), typeof(double?),
        typeof(float), typeof(float?),
        typeof(Guid), typeof(Guid?),
    ];

    public static bool IsScalarType(Type type) => ScalarTypes.Contains(Nullable.GetUnderlyingType(type) ?? type);

    public static bool IsNavigationProperty(PropertyInfo prop)
    {
        var type = prop.PropertyType;
        if (type == typeof(string))
        {
            return false;
        }

        if (typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string))
        {
            return true;
        }

        return type.IsClass;
    }

    public static object? GetPropertyValue(object entity, string propertyName)
    {
        var prop = entity.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        return prop?.GetValue(entity);
    }

    public static string? GetPrimaryKey(object entity, LuceneEntityTypeDefinition definition)
    {
        var parts = new List<string>();
        foreach (var name in definition.PrimaryKeyProperties)
        {
            var value = GetPropertyValue(entity, name);
            if (value is null)
            {
                return null;
            }

            parts.Add(value.ToString() ?? string.Empty);
        }

        return parts.Count == 0 ? null : string.Join("_", parts);
    }

    public static string CollectSearchText(
        object entity,
        LuceneEntityTypeDefinition definition,
        int maxDepth,
        HashSet<object>? visited = null)
    {
        visited ??= new HashSet<object>(ReferenceEqualityComparer.Instance);
        if (!visited.Add(entity))
        {
            return string.Empty;
        }

        var exclude = new HashSet<string>(definition.ExcludeProperties, StringComparer.OrdinalIgnoreCase);
        var sb = new StringBuilder();
        AppendScalars(entity, exclude, sb);

        if (maxDepth > 0)
        {
            AppendNavigations(entity, definition, maxDepth, visited, exclude, sb);
        }

        return sb.ToString().Trim();
    }

    private static void AppendScalars(object entity, HashSet<string> exclude, StringBuilder sb)
    {
        foreach (var prop in entity.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!prop.CanRead || prop.GetIndexParameters().Length > 0 || exclude.Contains(prop.Name))
            {
                continue;
            }

            if (!IsScalarType(prop.PropertyType))
            {
                continue;
            }

            AppendScalarValue(sb, prop.GetValue(entity));
        }
    }

    private static void AppendScalarValue(StringBuilder sb, object? value)
    {
        if (value is not string s)
        {
            AppendPart(sb, value?.ToString());
            return;
        }

        AppendPart(sb, s);
        var flattened = FlattenJsonForSearch(s);
        if (!string.IsNullOrWhiteSpace(flattened))
        {
            AppendPart(sb, flattened);
        }
    }

    /// <summary>Pulls primitive values out of JSON columns (e.g. EstimatesInvolved) for token-friendly search text.</summary>
    private static string? FlattenJsonForSearch(string value)
    {
        var trimmed = value.Trim();
        if (trimmed.Length == 0 || (trimmed[0] != '[' && trimmed[0] != '{'))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(trimmed);
            var sb = new StringBuilder();
            CollectJsonPrimitives(document.RootElement, sb);
            return sb.Length == 0 ? null : sb.ToString().Trim();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static void CollectJsonPrimitives(JsonElement element, StringBuilder sb)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    CollectJsonPrimitives(property.Value, sb);
                }

                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    CollectJsonPrimitives(item, sb);
                }

                break;
            case JsonValueKind.String:
                AppendPart(sb, element.GetString());
                break;
            case JsonValueKind.Number:
            case JsonValueKind.True:
            case JsonValueKind.False:
                AppendPart(sb, element.ToString());
                break;
        }
    }

    private static void AppendNavigations(
        object entity,
        LuceneEntityTypeDefinition definition,
        int maxDepth,
        HashSet<object> visited,
        HashSet<string> exclude,
        StringBuilder sb)
    {
        foreach (var prop in entity.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!prop.CanRead || prop.GetIndexParameters().Length > 0 || exclude.Contains(prop.Name))
            {
                continue;
            }

            if (!IsNavigationProperty(prop))
            {
                continue;
            }

            var value = prop.GetValue(entity);
            if (value is null)
            {
                continue;
            }

            if (value is string)
            {
                continue;
            }

            if (value is IEnumerable enumerable and not string)
            {
                foreach (var item in enumerable)
                {
                    if (item is null)
                    {
                        continue;
                    }

                    if (IsScalarType(item.GetType()))
                    {
                        AppendPart(sb, item.ToString());
                    }
                    else
                    {
                        sb.Append(CollectSearchText(item, definition, maxDepth - 1, visited));
                    }
                }

                continue;
            }

            sb.Append(CollectSearchText(value, definition, maxDepth - 1, visited));
        }
    }

    public static string ResolveTitle(object entity, LuceneEntityTypeDefinition definition)
    {
        string? raw = null;
        if (!string.IsNullOrWhiteSpace(definition.TitleProperty))
        {
            raw = GetPropertyValue(entity, definition.TitleProperty)?.ToString();
        }
        else
        {
            foreach (var candidate in new[] { "Title", "Name", "Text", "Code", "Email", "UserId", "InvoiceNumber", "ProfitCenter", "BreakoutName" })
            {
                raw = GetPropertyValue(entity, candidate)?.ToString();
                if (!string.IsNullOrWhiteSpace(raw))
                {
                    break;
                }
            }
        }

        if (string.IsNullOrWhiteSpace(raw) && !string.IsNullOrWhiteSpace(definition.TitleWhenEmpty))
        {
            return RouteTemplateResolver.Resolve(definition.TitleWhenEmpty, entity) ?? definition.TypeName;
        }

        raw ??= definition.TypeName;
        return Truncate(raw, definition.TitleMaxLength);
    }

    public static string? ResolveSubtitle(object entity, LuceneEntityTypeDefinition definition)
    {
        if (string.IsNullOrWhiteSpace(definition.SubtitleProperty))
        {
            return null;
        }

        return GetPropertyValue(entity, definition.SubtitleProperty)?.ToString();
    }

    public static DateTime? ResolveUpdatedOn(object entity, LuceneEntityTypeDefinition definition)
    {
        var name = string.IsNullOrWhiteSpace(definition.UpdatedOnProperty)
            ? "UpdatedOn"
            : definition.UpdatedOnProperty;

        var value = GetPropertyValue(entity, name);
        if (value is DateTime dt)
        {
            return dt;
        }

        var created = GetPropertyValue(entity, "CreatedOn");
        return created is DateTime createdDt ? createdDt : null;
    }

    public static int ResolveMaxDepth(LuceneEntityTypeDefinition definition) =>
        definition.MaxNavigationDepth > 0
            ? definition.MaxNavigationDepth
            : definition.MappingMode switch
            {
                LuceneMappingMode.Simple => 0,
                LuceneMappingMode.Entity => 1,
                LuceneMappingMode.DomainObject => 3,
                _ => 0,
            };

    private static string Truncate(string value, int maxLength)
    {
        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength] + "…";
    }

    private static void AppendPart(StringBuilder sb, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        if (sb.Length > 0)
        {
            sb.Append(' ');
        }

        sb.Append(value.Trim());
    }
}

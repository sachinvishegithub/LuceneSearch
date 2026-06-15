using System.Text;
using System.Text.RegularExpressions;

namespace LuceneSearchHelper.Mapping;

internal static partial class RouteTemplateResolver
{
    private static readonly Regex PlaceholderRegex = PlaceholderPattern();

    public static string? Resolve(string? template, object entity)
    {
        if (string.IsNullOrWhiteSpace(template))
        {
            return null;
        }

        return PlaceholderRegex.Replace(template, match =>
        {
            var propertyName = match.Groups[1].Value;
            var value = ReflectionMappingHelper.GetPropertyValue(entity, propertyName);
            return value?.ToString() ?? string.Empty;
        });
    }

    [GeneratedRegex(@"\{(\w+)\}", RegexOptions.CultureInvariant)]
    private static partial Regex PlaceholderPattern();
}

using Lucene.Net.Analysis;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Util;
using LuceneSearchHelper.Configuration;
using Microsoft.Extensions.Options;

namespace LuceneSearchHelper.Services;

public sealed class LuceneQueryBuilder
{
    private readonly LuceneSearchQueryOptions _options;

    public LuceneQueryBuilder(IOptions<LuceneOptions> luceneOptions)
    {
        _options = luceneOptions.Value.Search;
    }

    public Query Build(string searchTerm, Analyzer analyzer)
    {
        var trimmed = searchTerm.Trim();
        var queryText = PrepareQueryText(trimmed);
        if (string.IsNullOrWhiteSpace(queryText))
        {
            queryText = QueryParser.Escape(trimmed);
        }

        var fields = _options.FieldBoosts.Keys.ToArray();
        var boosts = _options.FieldBoosts.ToDictionary(k => k.Key, v => v.Value, StringComparer.OrdinalIgnoreCase);

        var tokenCount = trimmed.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
        var parser = new MultiFieldQueryParser(LuceneVersion.LUCENE_48, fields, analyzer, boosts)
        {
            // OR so terms can match in different fields/chunks (e.g. JSON estimate ids in searchText).
            DefaultOperator = tokenCount > 1 ? QueryParser.OR_OPERATOR : QueryParser.AND_OPERATOR,
            FuzzyMinSim = _options.FuzzyMinSimilarity,
        };

        // PrepareQueryText already escapes literals and adds fuzzy/proximity syntax — do not escape again.
        return parser.Parse(queryText);
    }

    private string PrepareQueryText(string input)
    {
        if (input.Contains('"', StringComparison.Ordinal))
        {
            var withFuzzy = _options.EnableFuzzySearch
                ? ApplyFuzzyToUnquotedSegments(input)
                : input;

            if (_options.EnableProximitySearch && IsQuotedPhrase(input))
            {
                return $"{withFuzzy}~{_options.ProximitySlop}";
            }

            return withFuzzy;
        }

        var tokens = input.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0)
        {
            return QueryParser.Escape(input);
        }

        if (tokens.Length == 1)
        {
            var token = QueryParser.Escape(tokens[0]);
            return _options.EnableFuzzySearch
                ? $"{token}~{FormatFuzzySimilarity(_options.FuzzyMinSimilarity)}"
                : token;
        }

        return _options.EnableFuzzySearch
            ? string.Join(' ', tokens.Select(t => $"{QueryParser.Escape(t)}~{FormatFuzzySimilarity(_options.FuzzyMinSimilarity)}"))
            : string.Join(' ', tokens.Select(QueryParser.Escape));
    }

    private static string FormatFuzzySimilarity(float similarity) =>
        similarity.ToString(System.Globalization.CultureInfo.InvariantCulture);

    private static bool IsQuotedPhrase(string input)
    {
        var trimmed = input.Trim();
        return trimmed.Length >= 2
            && trimmed[0] == '"'
            && trimmed[^1] == '"'
            && !trimmed.Contains('~', StringComparison.Ordinal);
    }

    private string ApplyFuzzyToUnquotedSegments(string input)
    {
        var result = new System.Text.StringBuilder();
        var inQuotes = false;
        var word = new System.Text.StringBuilder();

        foreach (var ch in input)
        {
            if (ch == '"')
            {
                FlushWord(result, word, inQuotes);
                inQuotes = !inQuotes;
                result.Append(ch);
                continue;
            }

            if (!inQuotes && char.IsWhiteSpace(ch))
            {
                FlushWord(result, word, inQuotes);
                if (result.Length > 0 && result[^1] != ' ')
                {
                    result.Append(' ');
                }

                continue;
            }

            word.Append(ch);
        }

        FlushWord(result, word, inQuotes);
        return result.ToString();
    }

    private void FlushWord(System.Text.StringBuilder result, System.Text.StringBuilder word, bool inQuotes)
    {
        if (word.Length == 0)
        {
            return;
        }

        var text = word.ToString();
        word.Clear();

        if (inQuotes || !_options.EnableFuzzySearch)
        {
            result.Append(inQuotes ? text : QueryParser.Escape(text));
            return;
        }

        result.Append(QueryParser.Escape(text));
        result.Append('~');
        result.Append(FormatFuzzySimilarity(_options.FuzzyMinSimilarity));
    }
}

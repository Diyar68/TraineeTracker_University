using System.Text.Json;

namespace TraineeTracker.Services;

public sealed class CurriculumJsonParser : ICurriculumJsonParser
{
    public async Task<IReadOnlyList<ImportedLection>> ParseAsync(Stream jsonStream, CancellationToken cancellationToken = default)
    {
        using var document = await JsonDocument.ParseAsync(jsonStream, cancellationToken: cancellationToken);
        var root = document.RootElement;
        var array = GetLessonArray(root);
        var lections = new List<ImportedLection>();
        var seenIds = new HashSet<long>();
        var orderIndex = 0;

        foreach (var lessonElement in array.EnumerateArray())
        {
            var id = RequiredInt(lessonElement, "id");
            if (!seenIds.Add(id))
            {
                throw new InvalidDataException($"Die Importdatei enthaelt die Lektions-ID '{id}' mehrfach.");
            }

            var title = RequiredString(lessonElement, "title");

            lections.Add(new ImportedLection(
                id,
                BuildDisplayNumber(orderIndex),
                title,
                OptionalDecimal(lessonElement, 0m, "estimate", "estimated_days", "estimatedEffortInPersonDays", "effort"),
                RequiredUrl(lessonElement, "url", "contentUrl", "href"),
                orderIndex++,
                OptionalBoolean(lessonElement, "deprecated", "isDeprecated")));
        }

        if (lections.Count == 0)
        {
            throw new InvalidDataException("Die Importdatei enthaelt keine Lektionen.");
        }

        return lections;
    }

    private static JsonElement GetLessonArray(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Array)
        {
            return root;
        }

        foreach (var property in new[] { "lessons", "cards", "curriculum" })
        {
            if (root.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.Array)
            {
                return value;
            }
        }

        throw new InvalidDataException("Die JSON-Datei muss ein Array oder ein Objekt mit 'lessons' enthalten.");
    }

    private static string RequiredString(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String)
            {
                var text = value.GetString();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    return text.Trim();
                }
            }
        }

        throw new InvalidDataException($"Pflichtfeld fehlt: {string.Join("/", names)}.");
    }
    
    private static string RequiredUrl(JsonElement element, params string[] names)
    {
        var url = RequiredString(element, names);

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidDataException($"Pflichtfeld ist keine gültige URL: {string.Join("/", names)}.");
        }

        return url;
    }

    private static long RequiredInt(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (!element.TryGetProperty(name, out var value))
            {
                continue;
            }

            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var number))
            {
                return number;
            }

            if (value.ValueKind == JsonValueKind.String &&
                long.TryParse(
                    value.GetString(),
                    System.Globalization.NumberStyles.Integer,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out number))
            {
                return number;
            }
        }

        throw new InvalidDataException($"Pflichtfeld fehlt oder ist keine ganze Zahl: {string.Join("/", names)}.");
    }

    private static decimal OptionalDecimal(JsonElement element, decimal fallback, params string[] names)
    {
        foreach (var name in names)
        {
            if (!element.TryGetProperty(name, out var value))
            {
                continue;
            }

            if (value.ValueKind == JsonValueKind.Null)
            {
                return fallback;
            }

            if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var number))
            {
                return number;
            }

            if (value.ValueKind == JsonValueKind.String &&
                decimal.TryParse(
                    value.GetString(),
                    System.Globalization.NumberStyles.AllowDecimalPoint,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out number))
            {
                return number;
            }
        }

        return fallback;
    }

    private static bool OptionalBoolean(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (element.TryGetProperty(name, out var value))
            {
                return value.ValueKind switch
                {
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    JsonValueKind.String => bool.TryParse(value.GetString(), out var parsed) && parsed,
                    _ => false,
                };
            }
        }

        return false;
    }

    private static string BuildDisplayNumber(int orderIndex)
    {
        return (orderIndex + 1).ToString("00", System.Globalization.CultureInfo.InvariantCulture);
    }
}

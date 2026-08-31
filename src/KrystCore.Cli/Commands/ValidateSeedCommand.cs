using System.Text.Json;

namespace KrystCore.Cli.Commands;

/// <summary>
/// Validates tenant seed JSON files against KrystCore schema requirements.
/// </summary>
public static class ValidateSeedCommand
{
    public static int Execute(string[] args)
    {
        if (args.Length == 0 || string.IsNullOrWhiteSpace(args[0]))
        {
            Console.Error.WriteLine("Error: Seed file path is required. Usage: kryst validate-seed <path>");
            return 1;
        }

        var filePath = Path.GetFullPath(args[0]);
        if (!File.Exists(filePath))
        {
            Console.Error.WriteLine($"[ERROR] File not found: {filePath}");
            return 1;
        }

        Console.WriteLine($"[VALIDATE] Validating seed file schema: {filePath}");
        try
        {
            var json = File.ReadAllText(filePath);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var errors = new List<string>();
            ValidateRootStructure(root, errors);
            ValidateEntityCollection(root, "Services", errors);
            ValidateEntityCollection(root, "Projects", errors);
            ValidateEntityCollection(root, "Clients", errors);
            ValidateEntityCollection(root, "Pages", errors);

            if (errors.Count > 0)
            {
                Console.Error.WriteLine($"[FAIL] Validation failed with {errors.Count} schema error(s):");
                foreach (var err in errors) Console.Error.WriteLine($"  - {err}");
                return 1;
            }

            Console.WriteLine("[PASS] Seed JSON file conforms to KrystCore schema.");
            return 0;
        }
        catch (JsonException ex)
        {
            Console.Error.WriteLine($"[FAIL] Invalid JSON syntax: {ex.Message}");
            return 1;
        }
    }

    private static void ValidateRootStructure(JsonElement root, List<string> errors)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            errors.Add("Root element must be a JSON object.");
            return;
        }

        var hasTenantName = TryGetProperty(root, "tenantName", out var name) ||
                            TryGetProperty(root, "TenantName", out name) ||
                            (TryGetProperty(root, "Company", out var comp) &&
                             (TryGetProperty(comp, "CommercialName", out name) || TryGetProperty(comp, "LegalName", out name)));

        if (!hasTenantName || string.IsNullOrWhiteSpace(name.GetString()))
        {
            errors.Add("Missing or empty tenant identifier/name in root or Company object.");
        }
    }

    private static void ValidateEntityCollection(JsonElement root, string propName, List<string> errors)
    {
        if (!TryGetProperty(root, propName, out var array)) return;
        if (array.ValueKind != JsonValueKind.Array)
        {
            errors.Add($"Property '{propName}' must be an array.");
            return;
        }

        var index = 0;
        foreach (var item in array.EnumerateArray())
        {
            if (!TryGetProperty(item, "Slug", out var slug) || string.IsNullOrWhiteSpace(slug.GetString()))
            {
                errors.Add($"Item #{index} in '{propName}' is missing required field 'Slug'.");
            }

            var hasNameOrTitle = (TryGetProperty(item, "Title", out var title) && !string.IsNullOrWhiteSpace(title.GetString())) ||
                                 (TryGetProperty(item, "Name", out var name) && !string.IsNullOrWhiteSpace(name.GetString()));

            if (!hasNameOrTitle)
            {
                errors.Add($"Item #{index} in '{propName}' is missing required name/title field ('Title' or 'Name').");
            }
            index++;
        }
    }

    private static bool TryGetProperty(JsonElement element, string name, out JsonElement value)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            value = default;
            return false;
        }

        if (element.TryGetProperty(name, out value)) return true;

        foreach (var prop in element.EnumerateObject())
        {
            if (string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = prop.Value;
                return true;
            }
        }

        value = default;
        return false;
    }
}

namespace KrystCore.Rules;

/// <summary>
/// Provides typed resolution of fields within an execution context dictionary.
/// </summary>
public static class RuleContextAccessor
{
    public static object? GetFieldValue(IReadOnlyDictionary<string, object?> context, string fieldName)
    {
        if (context == null || string.IsNullOrEmpty(fieldName))
        {
            return null;
        }

        return context.TryGetValue(fieldName, out var value) ? value : null;
    }
}

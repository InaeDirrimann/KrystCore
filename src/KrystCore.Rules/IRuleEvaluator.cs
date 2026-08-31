using System.Text.Json;

namespace KrystCore.Rules;

/// <summary>
/// Evaluates sandboxed rule AST expressions against an execution context.
/// </summary>
public interface IRuleEvaluator
{
    /// <summary>
    /// Evaluates the parsed rule AST against the provided context dictionary.
    /// </summary>
    /// <param name="ast">The root JsonElement representing the rule AST.</param>
    /// <param name="context">The contextual key-value pairs for field resolution.</param>
    /// <returns>True if the rule passes, false otherwise.</returns>
    bool Evaluate(JsonElement ast, IReadOnlyDictionary<string, object?> context);
}

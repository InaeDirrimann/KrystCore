using System.Collections.Concurrent;
using System.Text.Json;


namespace KrystCore.Rules;

/// <summary>
/// Thread-safe evaluator that compiles and caches rule expressions.
/// </summary>
public class RuleEvaluator : IRuleEvaluator
{
    private readonly ConcurrentDictionary<string, Func<IReadOnlyDictionary<string, object?>, bool>> _cache = new();

    public bool Evaluate(JsonElement ast, IReadOnlyDictionary<string, object?> context)
    {
        var rawJson = ast.GetRawText();
        var compiled = _cache.GetOrAdd(rawJson, _ => RuleExpressionCompiler.CompileRule(ast));
        return compiled(context);
    }
}

using System.Linq.Expressions;
using System.Text.Json;

namespace KrystCore.Rules;

/// <summary>
/// Compiles sandboxed JSON AST rule definitions into strongly typed expression trees.
/// Hard depth cap of 32 enforced to eliminate stack overflows.
/// </summary>
public static class RuleExpressionCompiler
{
    private const int MaxAstDepth = 32;
    private static readonly System.Reflection.MethodInfo ConvertToDecimalMethod =
        typeof(Convert).GetMethod(nameof(Convert.ToDecimal), [typeof(object)])!;
    private static readonly System.Reflection.MethodInfo GetFieldValueMethod =
        typeof(RuleContextAccessor).GetMethod(nameof(RuleContextAccessor.GetFieldValue))!;

    public static Func<IReadOnlyDictionary<string, object?>, bool> CompileRule(JsonElement ast)
    {
        var contextParam = Expression.Parameter(typeof(IReadOnlyDictionary<string, object?>), "ctx");
        var body = ParseNode(ast, contextParam, depth: 0);
        return Expression.Lambda<Func<IReadOnlyDictionary<string, object?>, bool>>(body, contextParam).Compile();
    }

    private static Expression ParseNode(JsonElement node, ParameterExpression ctx, int depth)
    {
        if (depth > MaxAstDepth)
        {
            throw new InvalidOperationException($"Rule AST depth exceeds maximum allowable limit of {MaxAstDepth}.");
        }

        if (!node.TryGetProperty("op", out var opProp))
        {
            throw new FormatException("Rule AST node must contain an 'op' property.");
        }

        var op = opProp.GetString()?.ToLowerInvariant();
        return op switch
        {
            "and" => ParseAnd(node, ctx, depth),
            "or" => ParseOr(node, ctx, depth),
            "not" => ParseNot(node, ctx, depth),
            "eq" => ParseBinaryComparison(node, ctx, depth, ExpressionType.Equal),
            "neq" => ParseBinaryComparison(node, ctx, depth, ExpressionType.NotEqual),
            "gt" => ParseNumericComparison(node, ctx, depth, ExpressionType.GreaterThan),
            "gte" => ParseNumericComparison(node, ctx, depth, ExpressionType.GreaterThanOrEqual),
            "lt" => ParseNumericComparison(node, ctx, depth, ExpressionType.LessThan),
            "lte" => ParseNumericComparison(node, ctx, depth, ExpressionType.LessThanOrEqual),
            _ => throw new NotSupportedException($"Unsupported rule operator: '{op}'")
        };
    }

    private static Expression ParseAnd(JsonElement node, ParameterExpression ctx, int depth)
    {
        var children = node.GetProperty("rules").EnumerateArray();
        Expression? current = null;
        foreach (var child in children)
        {
            var parsedChild = ParseNode(child, ctx, depth + 1);
            current = current == null ? parsedChild : Expression.AndAlso(current, parsedChild);
        }
        return current ?? Expression.Constant(true);
    }

    private static Expression ParseOr(JsonElement node, ParameterExpression ctx, int depth)
    {
        var children = node.GetProperty("rules").EnumerateArray();
        Expression? current = null;
        foreach (var child in children)
        {
            var parsedChild = ParseNode(child, ctx, depth + 1);
            current = current == null ? parsedChild : Expression.OrElse(current, parsedChild);
        }
        return current ?? Expression.Constant(false);
    }

    private static Expression ParseNot(JsonElement node, ParameterExpression ctx, int depth)
    {
        var child = node.GetProperty("rule");
        return Expression.Not(ParseNode(child, ctx, depth + 1));
    }

    private static Expression ParseBinaryComparison(JsonElement node, ParameterExpression ctx, int depth, ExpressionType expType)
    {
        var left = ParseValueNode(node.GetProperty("left"), ctx, depth + 1);
        var right = ParseValueNode(node.GetProperty("right"), ctx, depth + 1);
        return expType == ExpressionType.Equal
            ? Expression.Equal(left, right)
            : Expression.NotEqual(left, right);
    }

    private static Expression ParseNumericComparison(JsonElement node, ParameterExpression ctx, int depth, ExpressionType expType)
    {
        var left = ParseNumericValue(node.GetProperty("left"), ctx, depth + 1);
        var right = ParseNumericValue(node.GetProperty("right"), ctx, depth + 1);
        return Expression.MakeBinary(expType, left, right);
    }

    private static Expression ParseValueNode(JsonElement valNode, ParameterExpression ctx, int depth)
    {
        if (depth > MaxAstDepth) throw new InvalidOperationException("AST depth exceeded.");
        if (valNode.TryGetProperty("field", out var fieldProp))
        {
            var fieldName = fieldProp.GetString() ?? string.Empty;
            return ExtractContextValue(ctx, fieldName, typeof(object));
        }
        if (valNode.TryGetProperty("literal", out var litProp))
        {
            return Expression.Constant(litProp.GetString());
        }
        return Expression.Constant(null, typeof(object));
    }

    private static Expression ParseNumericValue(JsonElement valNode, ParameterExpression ctx, int depth)
    {
        if (depth > MaxAstDepth) throw new InvalidOperationException("AST depth exceeded.");
        if (valNode.TryGetProperty("field", out var fieldProp))
        {
            var fieldName = fieldProp.GetString() ?? string.Empty;
            var objExpr = ExtractContextValue(ctx, fieldName, typeof(object));
            return Expression.Call(ConvertToDecimalMethod, objExpr);
        }
        if (valNode.TryGetProperty("literal", out var litProp))
        {
            return Expression.Constant(litProp.GetDecimal());
        }
        return Expression.Constant(0m);
    }

    private static Expression ExtractContextValue(ParameterExpression ctx, string fieldName, Type targetType)
    {
        return Expression.Call(GetFieldValueMethod, ctx, Expression.Constant(fieldName));
    }
}

import type {
  RuleAstNode,
  RuleContext,
  RuleValueNode,
} from './types.ts';

const DEFAULT_MAX_AST_DEPTH = 32;

/**
 * Resolves an AST value node to its runtime context value or literal.
 */
function resolveValue(node: RuleValueNode, context: RuleContext): unknown {
  if ('field' in node && typeof node.field === 'string') {
    return context[node.field];
  }
  if ('literal' in node) {
    return node.literal;
  }
  return undefined;
}

/**
 * Resolves and coerces an AST value node to a numeric value.
 */
function resolveNumericValue(node: RuleValueNode, context: RuleContext): number {
  const val = resolveValue(node, context);
  if (val === null || val === undefined) return 0;
  if (typeof val === 'number') return Number.isNaN(val) ? 0 : val;
  if (typeof val === 'bigint') return Number(val);
  const parsed = Number(val);
  return Number.isNaN(parsed) ? 0 : parsed;
}

/**
 * Recursively executes an AST node against a context dictionary.
 */
function executeNode(node: RuleAstNode, context: RuleContext, depth: number, maxDepth: number): boolean {
  if (depth > maxDepth) {
    throw new Error(`Rule AST depth exceeds maximum allowable limit of ${maxDepth}.`);
  }

  switch (node.op) {
    case 'and':
      return !node.rules || node.rules.length === 0
        ? true
        : node.rules.every((child) => executeNode(child, context, depth + 1, maxDepth));

    case 'or':
      return !node.rules || node.rules.length === 0
        ? false
        : node.rules.some((child) => executeNode(child, context, depth + 1, maxDepth));

    case 'not':
      return !executeNode(node.rule, context, depth + 1, maxDepth);

    case 'eq': {
      const l = resolveValue(node.left, context);
      const r = resolveValue(node.right, context);
      return l === r;
    }

    case 'neq': {
      const l = resolveValue(node.left, context);
      const r = resolveValue(node.right, context);
      return l !== r;
    }

    case 'gt': {
      const l = resolveNumericValue(node.left, context);
      const r = resolveNumericValue(node.right, context);
      return l > r;
    }

    case 'gte': {
      const l = resolveNumericValue(node.left, context);
      const r = resolveNumericValue(node.right, context);
      return l >= r;
    }

    case 'lt': {
      const l = resolveNumericValue(node.left, context);
      const r = resolveNumericValue(node.right, context);
      return l < r;
    }

    case 'lte': {
      const l = resolveNumericValue(node.left, context);
      const r = resolveNumericValue(node.right, context);
      return l <= r;
    }

    default:
      throw new Error(`Unsupported rule operator: ${(node as { op: string }).op}`);
  }
}

/**
 * Evaluates an AST rule synchronously against an execution context dictionary.
 */
export function evaluateRule(
  ast: RuleAstNode,
  context: RuleContext,
  maxDepth = DEFAULT_MAX_AST_DEPTH
): boolean {
  return executeNode(ast, context, 0, maxDepth);
}

/**
 * Pre-compiles an AST rule into a high-performance evaluation function.
 */
export function compileRule(
  ast: RuleAstNode,
  maxDepth = DEFAULT_MAX_AST_DEPTH
): (context: RuleContext) => boolean {
  return (context: RuleContext) => executeNode(ast, context, 0, maxDepth);
}

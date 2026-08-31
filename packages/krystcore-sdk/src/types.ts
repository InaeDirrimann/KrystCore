/**
 * Strongly typed dynamic content entity stored in generic table with JSON payload.
 */
export interface Content<TData = Record<string, unknown>> {
  id: string;
  tenantId?: string | null;
  contentTypeCode: string;
  slug: string;
  title: string;
  data: TData;
  createdAtUtc: string;
  updatedAtUtc: string;
}

/**
 * Payload to create dynamic content item.
 */
export interface ContentCreateRequest<TData = Record<string, unknown>> {
  slug: string;
  title: string;
  data?: TData;
}

/**
 * Payload to update dynamic content item.
 */
export interface ContentUpdateRequest<TData = Record<string, unknown>> {
  slug?: string;
  title?: string;
  data?: TData;
}

/**
 * Immutable audit trail capturing entity mutations and security events.
 */
export interface AuditLog {
  id: string;
  tenantId?: string | null;
  userId?: string | null;
  action: string;
  entityName: string;
  entityId: string;
  detailsJson?: string | null;
  timestampUtc: string;
}

/**
 * User account entity representing an authenticated identity.
 */
export interface User {
  id: string;
  tenantId?: string | null;
  email: string;
  roleBitmask: string | bigint | number;
  isActive: boolean;
  createdAtUtc: string;
}

/**
 * Pagination query parameters for listing endpoints.
 */
export interface PaginationParams {
  page?: number;
  pageSize?: number;
}

/**
 * Query filter parameters.
 */
export type FilterParams = Record<string, string | number | boolean | undefined | null>;

/**
 * Client initialization options.
 */
export interface ClientOptions {
  baseUrl: string;
  headers?: Record<string, string>;
  cookie?: string;
  fetch?: typeof fetch;
}

/**
 * Value operand in rule AST (either field lookup or literal value).
 */
export type RuleValueNode =
  | { field: string; literal?: never }
  | { literal: string | number | boolean | null; field?: never };

/**
 * Composite AND / OR logical rule node.
 */
export interface RuleLogicalNode {
  op: 'and' | 'or';
  rules: RuleAstNode[];
}

/**
 * Negation logical rule node.
 */
export interface RuleNotNode {
  op: 'not';
  rule: RuleAstNode;
}

/**
 * Binary equality/inequality comparison node.
 */
export interface RuleBinaryComparisonNode {
  op: 'eq' | 'neq';
  left: RuleValueNode;
  right: RuleValueNode;
}

/**
 * Numeric inequality comparison node.
 */
export interface RuleNumericComparisonNode {
  op: 'gt' | 'gte' | 'lt' | 'lte';
  left: RuleValueNode;
  right: RuleValueNode;
}

/**
 * Closed AST rule definition.
 */
export type RuleAstNode =
  | RuleLogicalNode
  | RuleNotNode
  | RuleBinaryComparisonNode
  | RuleNumericComparisonNode;

/**
 * Context dictionary for AST rule execution.
 */
export type RuleContext = Record<string, unknown>;

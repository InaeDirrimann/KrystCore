# @krystcore/sdk

High-performance, zero-dependency TypeScript SDK for the KrystCore™ enterprise engine.

## Features

- **Zero Runtime Dependencies**: Built with pure modern TypeScript / ES2022.
- **64-bit Permission Parity**: Bitwise authorization math matching the C# engine kernel 1:1.
- **Offline AST Rule Evaluator**: Executes sandboxed JSON rule expressions in browser/Node with zero network latency.
- **Dynamic Content CRUD**: Full support for Path 2 generic dynamic content schemas (`/api/v1/content/{type}`).
- **Immutable Client**: Method chaining for auth tokens, cookies, permissions, and custom headers.

## Installation

```bash
npm install @krystcore/sdk
```

## Quickstart

### 1. Dynamic Content CRUD

```typescript
import { KrystClient, PermissionConstants } from '@krystcore/sdk';

const client = new KrystClient({
  baseUrl: 'https://api.yourdomain.com',
})
  .withPermissions(PermissionConstants.FullAdmin)
  .withUserId('usr_12345');

// Query dynamic content
const items = await client.getContentList('equipment', { page: 1, pageSize: 20 });

// Create dynamic item
const created = await client.createContent('equipment', {
  slug: 'crane-50t',
  title: 'Heavy Crane 50T',
  data: { capacityTon: 50, manufacturer: 'Liebherr' },
});

// Update item
await client.updateContent('equipment', 'crane-50t', {
  title: 'Heavy Crane 50T - Updated',
});

// Delete item
await client.deleteContent('equipment', 'crane-50t');
```

### 2. 64-bit Permission Bitmasks

```typescript
import { PermissionBitmask, PermissionConstants } from '@krystcore/sdk';

const mask = new PermissionBitmask(PermissionConstants.View | PermissionConstants.Create);

mask.hasPermission(PermissionConstants.View); // true
mask.hasPermission(PermissionConstants.Delete); // false

const adminMask = mask.add(PermissionConstants.Delete);
adminMask.hasPermission(PermissionConstants.Delete); // true

// Parity with C# formatting
console.log(mask.toString()); // 0x0000000000000003
```

### 3. Offline AST Rule Engine

```typescript
import { evaluateRule, compileRule, type RuleAstNode } from '@krystcore/sdk';

const rule: RuleAstNode = {
  op: 'and',
  rules: [
    { op: 'eq', left: { field: 'category' }, right: { literal: 'Civil' } },
    { op: 'gt', left: { field: 'budget' }, right: { literal: 50000 } },
  ],
};

const context = { category: 'Civil', budget: 75000 };

// Direct execution
const isAllowed = evaluateRule(rule, context); // true

// Pre-compiled fast path
const check = compileRule(rule);
const pass = check(context); // true
```

## License

Apache-2.0

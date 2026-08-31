import test from 'node:test';
import assert from 'node:assert/strict';
import {
  KrystClient,
  KrystApiError,
  PermissionBitmask,
  PermissionConstants,
  evaluateRule,
  compileRule,
} from '../src/index.ts';
import type { RuleAstNode } from '../src/types.ts';

test('PermissionBitmask: Bitwise Operations & Parity', () => {
  const mask = new PermissionBitmask(PermissionConstants.View | PermissionConstants.Create);

  assert.equal(mask.hasPermission(PermissionConstants.View), true);
  assert.equal(mask.hasPermission(PermissionConstants.Create), true);
  assert.equal(mask.hasPermission(PermissionConstants.Delete), false);

  const updated = mask.add(PermissionConstants.Delete);
  assert.equal(updated.hasPermission(PermissionConstants.Delete), true);
  assert.equal(mask.hasPermission(PermissionConstants.Delete), false); // Immutability

  const removed = updated.remove(PermissionConstants.View);
  assert.equal(removed.hasPermission(PermissionConstants.View), false);
  assert.equal(removed.hasPermission(PermissionConstants.Create), true);
  assert.equal(removed.hasPermission(PermissionConstants.Delete), true);
});

test('PermissionBitmask: Combinations, Hex formatting, and Parsing', () => {
  const readOnly = new PermissionBitmask(PermissionConstants.View);
  const writeOnly = new PermissionBitmask(PermissionConstants.Edit);

  const combined = readOnly.combine(writeOnly);
  assert.equal(combined.hasPermission(PermissionConstants.View), true);
  assert.equal(combined.hasPermission(PermissionConstants.Edit), true);

  const intersection = combined.intersect(readOnly);
  assert.equal(intersection.equals(readOnly), true);

  const fullAdmin = new PermissionBitmask(PermissionConstants.FullAdmin);
  assert.equal(fullAdmin.hasPermission(PermissionConstants.ViewAudit), true);
  assert.equal(fullAdmin.hasPermission(PermissionConstants.ExecuteWorkflow), true);

  const hexString = readOnly.toString();
  assert.equal(hexString, '0x0000000000000001');

  const parsed = PermissionBitmask.fromHex('0x0000000000000001');
  assert.equal(parsed.equals(readOnly), true);
});

test('Rule Evaluator: Numeric Comparison (gte / lt)', () => {
  const ast: RuleAstNode = {
    op: 'gte',
    left: { field: 'amount' },
    right: { literal: 1000 },
  };

  assert.equal(evaluateRule(ast, { amount: 1500 }), true);
  assert.equal(evaluateRule(ast, { amount: 1000 }), true);
  assert.equal(evaluateRule(ast, { amount: 500 }), false);

  const compiled = compileRule(ast);
  assert.equal(compiled({ amount: 2000 }), true);
  assert.equal(compiled({ amount: 999 }), false);
});

test('Rule Evaluator: Composite AND / OR / NOT', () => {
  const ast: RuleAstNode = {
    op: 'and',
    rules: [
      {
        op: 'eq',
        left: { field: 'category' },
        right: { literal: 'Civil' },
      },
      {
        op: 'gt',
        left: { field: 'budget' },
        right: { literal: 50000 },
      },
    ],
  };

  assert.equal(evaluateRule(ast, { category: 'Civil', budget: 75000 }), true);
  assert.equal(evaluateRule(ast, { category: 'Electrical', budget: 100000 }), false);
  assert.equal(evaluateRule(ast, { category: 'Civil', budget: 30000 }), false);

  const notAst: RuleAstNode = {
    op: 'not',
    rule: ast,
  };
  assert.equal(evaluateRule(notAst, { category: 'Civil', budget: 75000 }), false);
  assert.equal(evaluateRule(notAst, { category: 'Electrical', budget: 100000 }), true);
});

test('Rule Evaluator: Max depth recursion guard', () => {
  let nested: RuleAstNode = {
    op: 'eq',
    left: { field: 'x' },
    right: { literal: 1 },
  };

  for (let i = 0; i < 40; i++) {
    nested = { op: 'not', rule: nested };
  }

  assert.throws(() => evaluateRule(nested, { x: 1 }), /exceeds maximum allowable limit/);
});

test('KrystClient: CRUD operations with custom mock fetch', async () => {
  const calls: { url: string; init?: RequestInit }[] = [];

  const mockFetch: typeof fetch = async (input, init) => {
    const url = String(input);
    calls.push({ url, init });

    if (url.includes('/api/v1/content/equipment') && init?.method === 'GET') {
      return new Response(JSON.stringify([{ id: '123', slug: 'crane-50t', title: 'Crane' }]), {
        status: 200,
        headers: { 'Content-Type': 'application/json' },
      });
    }

    if (url.includes('/api/v1/content/equipment') && init?.method === 'POST') {
      return new Response(JSON.stringify({ id: '124', slug: 'loader-10t', title: 'Loader' }), {
        status: 201,
        headers: { 'Content-Type': 'application/json' },
      });
    }

    if (url.includes('/api/v1/content/equipment/123') && init?.method === 'PUT') {
      return new Response(JSON.stringify({ id: '123', slug: 'crane-50t', title: 'Updated Crane' }), {
        status: 200,
        headers: { 'Content-Type': 'application/json' },
      });
    }

    if (url.includes('/api/v1/content/equipment/123') && init?.method === 'DELETE') {
      return new Response(null, { status: 204 });
    }

    return new Response(JSON.stringify({ error: 'Not Found' }), { status: 404 });
  };

  const client = new KrystClient({
    baseUrl: 'https://engine.internal:5001',
    cookie: 'session=auth-xyz',
    fetch: mockFetch,
  })
    .withPermissions(PermissionConstants.FullAdmin)
    .withUserId('user-100');

  // GET list
  const list = await client.getContentList('equipment', { page: 1, pageSize: 10 });
  assert.equal(list.length, 1);
  assert.equal(list[0]?.slug, 'crane-50t');

  const getCall = calls[0]!;
  assert.match(getCall.url, /page=1/);
  const getHeaders = new Headers(getCall.init?.headers);
  assert.equal(getHeaders.get('X-User-Permissions'), PermissionConstants.FullAdmin.toString());
  assert.equal(getHeaders.get('X-User-Id'), 'user-100');
  assert.equal(getHeaders.get('Cookie'), 'session=auth-xyz');

  // POST create
  const created = await client.createContent('equipment', {
    slug: 'loader-10t',
    title: 'Loader',
    data: { capacityTon: 10 },
  });
  assert.equal(created.id, '124');

  // PUT update
  const updated = await client.updateContent('equipment', '123', {
    title: 'Updated Crane',
  });
  assert.equal(updated.title, 'Updated Crane');

  // DELETE
  const deleted = await client.deleteContent('equipment', '123');
  assert.equal(deleted, true);
});

test('KrystClient: Throws KrystApiError on HTTP failure', async () => {
  const mockFetch: typeof fetch = async () => {
    return new Response(JSON.stringify({ error: 'Forbidden: Insufficient permissions.' }), {
      status: 403,
      statusText: 'Forbidden',
      headers: { 'Content-Type': 'application/json' },
    });
  };

  const client = new KrystClient({
    baseUrl: 'https://engine.internal:5001',
    fetch: mockFetch,
  });

  await assert.rejects(
    () => client.getContentList('restricted-type'),
    (err: unknown) => {
      assert(err instanceof KrystApiError);
      assert.equal(err.status, 403);
      assert.match(err.message, /Forbidden: Insufficient permissions/);
      return true;
    }
  );
});

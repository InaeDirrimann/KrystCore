# KrystCore™ Open-Source Developer Tools & SDK

> High-performance, zero-dependency developer tools and SDKs for the KrystCore™ ecosystem.

[![License](https://img.shields.io/badge/License-Apache_2.0-blue.svg)](https://opensource.org/licenses/Apache-2.0)
[![TypeScript SDK](https://img.shields.io/badge/TypeScript%20SDK-Passing-brightgreen.svg)](packages/krystcore-sdk)
[![.NET CLI](https://img.shields.io/badge/.NET%20CLI-Passing-brightgreen.svg)](src/KrystCore.Cli)

---

## 📦 What's Inside This Repository

This repository contains official open-source developer tooling and client libraries under the **Apache-2.0 License**:

### 1. [`@krystcore/sdk`](packages/krystcore-sdk) (TypeScript / Next.js SDK)
- **Zero Runtime Dependencies:** Native `fetch` with strict TypeScript typing.
- **Dynamic Content CRUD:** Client for Path 2 `/api/v1/content/{type}` endpoints.
- **Client-Side AST Rule Evaluator:** Evaluates JSON rule trees in browser RAM in `< 1ms`.
- **PermissionBitmask:** 1:1 BigInt 64-bit bitmask evaluator matching C# struct arithmetic.

```bash
npm install @krystcore/sdk
```

### 2. [`KrystCore.Cli`](src/KrystCore.Cli) (Cross-Platform .NET CLI)
- `kryst new-tenant <Name>`: Scaffolds a new isolated `[Name].TenantPack` in **< 50ms**.
- `kryst audit <dir>`: Scans source directories for Ring 0 isolation breaches.
- `kryst validate-seed <path>`: Pre-flight JSON seed schema validator.

### 3. [`KrystCore.Rules`](src/KrystCore.Rules) (Sandboxed C# AST Rule Compiler)
- Compiles JSON AST trees into native machine delegates via `System.Linq.Expressions`.
- Enforces hard recursion depth guards (`depth <= 32`) to eliminate stack overflows.

---

## 📜 License

Licensed under the **Apache License, Version 2.0**. See [`LICENSE`](LICENSE) for details.

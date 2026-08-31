# KrystCore.Cli

High-performance, zero-fluff developer CLI for scaffolding, auditing, and validating tenants within the KrystCore™ ecosystem.

## Installation & Build

```bash
dotnet build src/KrystCore.Cli/KrystCore.Cli.csproj
```

Or run directly:

```bash
dotnet run --project src/KrystCore.Cli -- <command>
```

## Commands

### 1. `new-tenant <Name> [outputDirectory]`
Scaffolds a complete `[Name].TenantPack` project in `< 50ms`.

```bash
dotnet run --project src/KrystCore.Cli -- new-tenant Logistics src/
```

Generated layout:
- `src/Logistics.TenantPack/Logistics.TenantPack.csproj`
- `src/Logistics.TenantPack/LogisticsSeedData.cs`
- `src/Logistics.TenantPack/LogisticsSeeder.cs`
- `src/Logistics.TenantPack/LogisticsSeedLoader.cs`
- `src/Logistics.TenantPack/Seeds/logistics_initial_seed.json`
- `src/Logistics.TenantPack/Data/redirects.json`

### 2. `audit [path]`
Performs a strict cognitive-grade scan on Ring 0 source files to detect tenant isolation breaches or forbidden client branding in the agnostic kernel.

```bash
dotnet run --project src/KrystCore.Cli -- audit src/KrystCore.Engine
```

### 3. `validate-seed <path>`
Parses and validates a tenant JSON seed file against schema and integrity rules.

```bash
dotnet run --project src/KrystCore.Cli -- validate-seed src/Logistics.TenantPack/Seeds/logistics_initial_seed.json
```

## License

Apache-2.0

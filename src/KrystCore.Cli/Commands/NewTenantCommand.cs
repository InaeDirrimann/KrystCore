using System.Diagnostics;

namespace KrystCore.Cli.Commands;

/// <summary>
/// Scaffolds a new isolated tenant pack project structure in under 50ms.
/// </summary>
public static class NewTenantCommand
{
    public static int Execute(string[] args)
    {
        if (args.Length == 0 || string.IsNullOrWhiteSpace(args[0]))
        {
            Console.Error.WriteLine("Error: Tenant name is required. Usage: kryst new-tenant <Name> [output-directory]");
            return 1;
        }

        var sw = Stopwatch.StartNew();
        var rawName = args[0].Trim();
        var tenantName = SanitizeTenantName(rawName);
        var targetBaseDir = args.Length > 1 ? args[1] : Directory.GetCurrentDirectory();
        var projectDir = Path.Combine(targetBaseDir, $"{tenantName}.TenantPack");

        try
        {
            Directory.CreateDirectory(projectDir);
            Directory.CreateDirectory(Path.Combine(projectDir, "Data"));
            Directory.CreateDirectory(Path.Combine(projectDir, "Seeds"));

            WriteCsproj(projectDir, tenantName);
            WriteSeedDataClass(projectDir, tenantName);
            WriteSeederClass(projectDir, tenantName);
            WriteSeedLoaderClass(projectDir, tenantName);
            WriteSampleSeedJson(projectDir, tenantName);
            WriteSampleRedirectsJson(projectDir);

            sw.Stop();
            Console.WriteLine($"[SUCCESS] Scaffolded {tenantName}.TenantPack at '{projectDir}' in {sw.ElapsedMilliseconds}ms.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[ERROR] Failed to scaffold tenant pack: {ex.Message}");
            return 1;
        }
    }

    private static string SanitizeTenantName(string name)
    {
        var clean = new string(name.Where(char.IsLetterOrDigit).ToArray());
        if (string.IsNullOrEmpty(clean)) throw new ArgumentException("Tenant name must contain alphanumeric characters.");
        return char.ToUpperInvariant(clean[0]) + clean[1..];
    }

    private static void WriteCsproj(string dir, string name)
    {
        var content = $"""
<Project Sdk="Microsoft.NET.Sdk">

  <ItemGroup>
    <ProjectReference Include="..\KrystCore.Engine\KrystCore.Engine.csproj" />
  </ItemGroup>

  <ItemGroup>
    <None Update="Data\**;Seeds\**" CopyToOutputDirectory="PreserveNewest" />
    <EmbeddedResource Include="Data\**;Seeds\**" />
  </ItemGroup>

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

</Project>
""";
        File.WriteAllText(Path.Combine(dir, $"{name}.TenantPack.csproj"), content);
    }

    private static void WriteSeedDataClass(string dir, string name)
    {
        var content = $$"""
using KrystCore.Engine.Entities.Core;

namespace {{name}}.TenantPack;

/// <summary>
/// Root container object representing the deserialized {{name}} tenant seed dataset.
/// </summary>
public sealed class {{name}}SeedData
{
    public string TenantId { get; set; } = string.Empty;
    public string TenantName { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public List<Service> Services { get; set; } = new();
    public List<Project> Projects { get; set; } = new();
    public List<Client> Clients { get; set; } = new();
    public List<Page> Pages { get; set; } = new();
}
""";
        File.WriteAllText(Path.Combine(dir, $"{name}SeedData.cs"), content);
    }

    private static void WriteSeederClass(string dir, string name)
    {
        var content = $$"""
using KrystCore.Engine.Data;

namespace {{name}}.TenantPack;

/// <summary>
/// Orchestrates initial data seeding for {{name}} tenant.
/// </summary>
public static class {{name}}Seeder
{
    public static Task<int> SeedAsync(EngineDbContext db, Guid? tenantId = null, CancellationToken ct = default)
    {
        return {{name}}SeedLoader.SeedAsync(db, tenantId, ct);
    }
}
""";
        File.WriteAllText(Path.Combine(dir, $"{name}Seeder.cs"), content);
    }

    private static void WriteSeedLoaderClass(string dir, string name)
    {
        var content = $$"""
using System.Text.Json;
using KrystCore.Engine.Data;
using Microsoft.EntityFrameworkCore;

namespace {{name}}.TenantPack;

/// <summary>
/// Deserializes and seeds the EngineDbContext with {{name}} domain data.
/// </summary>
public static class {{name}}SeedLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static {{name}}SeedData LoadSeedData(string? customPath = null)
    {
        var path = customPath ?? Path.Combine(AppContext.BaseDirectory, "Seeds", "{{name.ToLowerInvariant()}}_initial_seed.json");
        var rawJson = File.ReadAllText(path);
        return JsonSerializer.Deserialize<{{name}}SeedData>(rawJson, JsonOptions)
               ?? throw new InvalidOperationException("Failed to deserialize {{name}} seed dataset.");
    }

    public static async Task<int> SeedAsync(EngineDbContext db, Guid? tenantId = null, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(db);
        var seed = LoadSeedData();
        var changesCount = 0;

        foreach (var service in seed.Services)
        {
            service.TenantId = tenantId;
            if (!await db.Services.AnyAsync(x => x.Slug == service.Slug, ct))
            {
                db.Services.Add(service);
                changesCount++;
            }
        }

        await db.SaveChangesAsync(ct);
        return changesCount;
    }
}
""";
        File.WriteAllText(Path.Combine(dir, $"{name}SeedLoader.cs"), content);
    }

    private static void WriteSampleSeedJson(string dir, string name)
    {
        var lower = name.ToLowerInvariant();
        var content = $$"""
{
  "tenantId": "00000000-0000-0000-0000-000000000001",
  "tenantName": "{{name}}",
  "domain": "{{lower}}.example.com",
  "services": [
    {
      "title": "General Contracting",
      "slug": "general-contracting",
      "description": "Comprehensive engineering and contracting services.",
      "displayOrder": 1,
      "isActive": true
    }
  ],
  "projects": [],
  "clients": [],
  "pages": []
}
""";
        File.WriteAllText(Path.Combine(dir, "Seeds", $"{lower}_initial_seed.json"), content);
    }

    private static void WriteSampleRedirectsJson(string dir)
    {
        var content = """
{
  "/old-path": "/new-path"
}
""";
        File.WriteAllText(Path.Combine(dir, "Data", "redirects.json"), content);
    }
}

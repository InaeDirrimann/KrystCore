namespace KrystCore.Cli.Commands;

/// <summary>
/// Scans Ring 0 source files for tenant domain isolation breaches.
/// </summary>
public static class AuditCommand
{
    private static readonly string[] ForbiddenTerms =
    [
        "aartco",
        "aartco-sa.com",
        "jouhi",
        "tenantpack"
    ];

    public static int Execute(string[] args)
    {
        var targetDir = ResolveEngineDirectory(args);
        if (!Directory.Exists(targetDir))
        {
            Console.Error.WriteLine($"[ERROR] Directory not found: {targetDir}");
            return 1;
        }

        Console.WriteLine($"[AUDIT] Scanning Ring 0 directory for isolation breaches: {targetDir}");
        var files = Directory.GetFiles(targetDir, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") &&
                        !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .ToArray();

        var violationCount = 0;
        foreach (var file in files)
        {
            violationCount += ScanFileForViolations(file);
        }

        if (violationCount > 0)
        {
            Console.Error.WriteLine($"[FAIL] Isolation audit failed with {violationCount} Ring 0 breach(es).");
            return 1;
        }

        Console.WriteLine($"[PASS] Isolation audit passed. 0 breaches found across {files.Length} files.");
        return 0;
    }

    private static string ResolveEngineDirectory(string[] args)
    {
        if (args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]))
        {
            return Path.GetFullPath(args[0]);
        }

        var current = Directory.GetCurrentDirectory();
        var candidate = Path.Combine(current, "src", "KrystCore.Engine");
        if (Directory.Exists(candidate)) return candidate;

        candidate = Path.Combine(current, "KrystCore.Engine");
        if (Directory.Exists(candidate)) return candidate;

        return current;
    }

    private static int ScanFileForViolations(string filePath)
    {
        var violations = 0;
        var lines = File.ReadAllLines(filePath);

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].AsSpan().Trim();
            if (line.IsEmpty || line.StartsWith("//") || line.StartsWith("/*")) continue;

            var lineLower = line.ToString().ToLowerInvariant();
            foreach (var term in ForbiddenTerms)
            {
                if (lineLower.Contains(term))
                {
                    Console.Error.WriteLine($"[BREACH] {filePath}:{i + 1} - Found forbidden tenant keyword '{term}'");
                    violations++;
                }
            }
        }

        return violations;
    }
}

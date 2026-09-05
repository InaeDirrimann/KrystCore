namespace KrystCore.Cli.Commands;

/// <summary>
/// Scans Ring 0 source files for tenant domain isolation breaches.
/// </summary>
public static class AuditCommand
{
    private static readonly string[] DefaultForbiddenTerms =
    [
        "tenantpack",
        "custom_client",
        "hardcoded_tenant"
    ];

    public static int Execute(string[] args)
    {
        var targetDir = ResolveEngineDirectory(args);
        var forbiddenTerms = ResolveForbiddenTerms(args);
        if (!Directory.Exists(targetDir))
        {
            Console.Error.WriteLine($"[ERROR] Directory not found: {targetDir}");
            return 1;
        }

        Console.WriteLine($"[AUDIT] Scanning directory for isolation breaches: {targetDir}");
        var files = Directory.GetFiles(targetDir, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") &&
                        !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .ToArray();

        var violationCount = 0;
        foreach (var file in files)
        {
            violationCount += ScanFileForViolations(file, forbiddenTerms);
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

    private static string[] ResolveForbiddenTerms(string[] args)
    {
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] == "--terms" && i + 1 < args.Length)
            {
                return args[i + 1].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(t => t.ToLowerInvariant()).ToArray();
            }
        }
        return DefaultForbiddenTerms;
    }

    private static int ScanFileForViolations(string filePath, string[] forbiddenTerms)
    {
        var violations = 0;
        var lineNumber = 0;
        var inBlockComment = false;

        foreach (var rawLine in File.ReadLines(filePath))
        {
            lineNumber++;
            var line = rawLine.AsSpan().Trim();
            if (line.IsEmpty) continue;

            if (inBlockComment)
            {
                var endIdx = line.IndexOf("*/".AsSpan(), StringComparison.Ordinal);
                if (endIdx >= 0)
                {
                    inBlockComment = false;
                    line = line[(endIdx + 2)..].Trim();
                    if (line.IsEmpty) continue;
                }
                else
                {
                    continue;
                }
            }

            if (line.StartsWith("//")) continue;

            if (line.StartsWith("/*"))
            {
                var endIdx = line.IndexOf("*/".AsSpan(), StringComparison.Ordinal);
                if (endIdx >= 0)
                {
                    line = line[(endIdx + 2)..].Trim();
                    if (line.IsEmpty) continue;
                }
                else
                {
                    inBlockComment = true;
                    continue;
                }
            }

            var lineLower = line.ToString().ToLowerInvariant();
            foreach (var term in forbiddenTerms)
            {
                if (lineLower.Contains(term))
                {
                    Console.Error.WriteLine($"[BREACH] {filePath}:{lineNumber} - Found forbidden keyword '{term}'");
                    violations++;
                }
            }
        }

        return violations;
    }
}

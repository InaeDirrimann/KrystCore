using KrystCore.Cli.Commands;

namespace KrystCore.Cli;

/// <summary>
/// High-speed command dispatcher for the KrystCore developer CLI.
/// </summary>
public static class Program
{
    public static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            PrintHelp();
            return 0;
        }

        var command = args[0].ToLowerInvariant();
        var cmdArgs = args.Length > 1 ? args[1..] : Array.Empty<string>();

        return command switch
        {
            "new-tenant" => NewTenantCommand.Execute(cmdArgs),
            "audit" => AuditCommand.Execute(cmdArgs),
            "validate-seed" => ValidateSeedCommand.Execute(cmdArgs),
            "help" or "--help" or "-h" => PrintHelp(),
            "version" or "--version" or "-v" => PrintVersion(),
            _ => HandleUnknownCommand(command)
        };
    }

    private static int PrintHelp()
    {
        Console.WriteLine("""
KrystCore CLI Tool v1.0.0
High-performance developer utilities for KrystCore ecosystem.

USAGE:
  kryst <command> [options]

COMMANDS:
  new-tenant <Name> [path]   Scaffolds a new [Name].TenantPack structure (< 50ms).
  audit [path]               Scans Ring 0 source files for isolation breaches.
  validate-seed <path>       Validates tenant seed JSON files against schema.
  version                    Displays CLI version.
  help                       Displays this help information.
""");
        return 0;
    }

    private static int PrintVersion()
    {
        Console.WriteLine("KrystCore CLI v1.0.0 (.NET 9.0)");
        return 0;
    }

    private static int HandleUnknownCommand(string command)
    {
        Console.Error.WriteLine($"[ERROR] Unknown command '{command}'. Run 'kryst help' for usage.");
        return 1;
    }
}

namespace KrystCore.Rules;

/// <summary>
/// Bit-shift indices and masks for standard engine operations.
/// </summary>
public static class PermissionConstants
{
    public const ulong None = 0UL;
    public const ulong View = 1UL << 0;
    public const ulong Create = 1UL << 1;
    public const ulong Edit = 1UL << 2;
    public const ulong Delete = 1UL << 3;
    public const ulong Publish = 1UL << 4;
    public const ulong Export = 1UL << 5;
    public const ulong ManageUsers = 1UL << 6;
    public const ulong ManageRoles = 1UL << 7;
    public const ulong ManageSettings = 1UL << 8;
    public const ulong ViewAudit = 1UL << 9;
    public const ulong ExecuteWorkflow = 1UL << 10;
    public const ulong FullAdmin = View | Create | Edit | Delete | Publish | Export |
                                   ManageUsers | ManageRoles | ManageSettings | ViewAudit | ExecuteWorkflow;
}

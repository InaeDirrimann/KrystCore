namespace KrystCore.Rules;

/// <summary>
/// Immutable 64-bit permission bitmask providing zero-allocation authorization checks.
/// </summary>
public readonly record struct PermissionBitmask(ulong Value) : IEquatable<PermissionBitmask>
{
    public bool HasPermission(ulong permissionFlag)
    {
        return (Value & permissionFlag) == permissionFlag;
    }

    public bool HasPermission(in PermissionBitmask required)
    {
        return (Value & required.Value) == required.Value;
    }

    public bool HasAnyPermission(ulong permissionFlag)
    {
        return (Value & permissionFlag) != 0;
    }

    public PermissionBitmask Add(ulong permissionFlag)
    {
        return new PermissionBitmask(Value | permissionFlag);
    }

    public PermissionBitmask Remove(ulong permissionFlag)
    {
        return new PermissionBitmask(Value & ~permissionFlag);
    }

    public static PermissionBitmask operator |(in PermissionBitmask left, in PermissionBitmask right)
        => new(left.Value | right.Value);

    public static PermissionBitmask operator &(in PermissionBitmask left, in PermissionBitmask right)
        => new(left.Value & right.Value);

    public static PermissionBitmask operator ~(in PermissionBitmask mask)
        => new(~mask.Value);

    public bool Equals(in PermissionBitmask other) => Value == other.Value;

    public override string ToString() => $"0x{Value:X16}";
}

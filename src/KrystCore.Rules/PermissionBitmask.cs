namespace KrystCore.Rules;

/// <summary>
/// Immutable 64-bit permission bitmask providing zero-allocation authorization checks.
/// </summary>
public readonly struct PermissionBitmask : IEquatable<PermissionBitmask>
{
    public ulong Value { get; }

    public PermissionBitmask(ulong value)
    {
        Value = value;
    }

    public bool HasPermission(ulong permissionFlag)
    {
        return (Value & permissionFlag) == permissionFlag;
    }

    public bool HasPermission(PermissionBitmask required)
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

    public static PermissionBitmask operator |(PermissionBitmask left, PermissionBitmask right)
        => new(left.Value | right.Value);

    public static PermissionBitmask operator &(PermissionBitmask left, PermissionBitmask right)
        => new(left.Value & right.Value);

    public static PermissionBitmask operator ~(PermissionBitmask mask)
        => new(~mask.Value);

    public static bool operator ==(PermissionBitmask left, PermissionBitmask right)
        => left.Value == right.Value;

    public static bool operator !=(PermissionBitmask left, PermissionBitmask right)
        => left.Value != right.Value;

    public bool Equals(PermissionBitmask other) => Value == other.Value;

    public override bool Equals(object? obj) => obj is PermissionBitmask other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => $"0x{Value:X16}";
}

/**
 * Bit-shift indices and masks for standard engine operations.
 * 1:1 Parity with C# PermissionConstants.
 */
export const PermissionConstants = {
  None: 0n,
  View: 1n << 0n,
  Create: 1n << 1n,
  Edit: 1n << 2n,
  Delete: 1n << 3n,
  Publish: 1n << 4n,
  Export: 1n << 5n,
  ManageUsers: 1n << 6n,
  ManageRoles: 1n << 7n,
  ManageSettings: 1n << 8n,
  ViewAudit: 1n << 9n,
  ExecuteWorkflow: 1n << 10n,
  get FullAdmin(): bigint {
    return (
      this.View |
      this.Create |
      this.Edit |
      this.Delete |
      this.Publish |
      this.Export |
      this.ManageUsers |
      this.ManageRoles |
      this.ManageSettings |
      this.ViewAudit |
      this.ExecuteWorkflow
    );
  },
} as const;

/**
 * Immutable 64-bit permission bitmask providing zero-overhead authorization checks.
 * 1:1 Parity with C# PermissionBitmask.
 */
export class PermissionBitmask {
  public readonly value: bigint;

  public constructor(value: bigint | number | string | PermissionBitmask = 0n) {
    if (value instanceof PermissionBitmask) {
      this.value = value.value;
    } else if (typeof value === 'bigint') {
      this.value = BigInt.asUintN(64, value);
    } else if (typeof value === 'number') {
      this.value = BigInt.asUintN(64, BigInt(value));
    } else if (typeof value === 'string') {
      const clean = value.trim();
      const parsed = clean.startsWith('0x') || clean.startsWith('0X')
        ? BigInt(clean)
        : BigInt(clean);
      this.value = BigInt.asUintN(64, parsed);
    } else {
      this.value = 0n;
    }
  }

  private static toBigIntValue(flag: bigint | number | string | PermissionBitmask): bigint {
    if (flag instanceof PermissionBitmask) return flag.value;
    if (typeof flag === 'bigint') return BigInt.asUintN(64, flag);
    if (typeof flag === 'number') return BigInt.asUintN(64, BigInt(flag));
    return BigInt.asUintN(64, BigInt(flag));
  }

  public hasPermission(flag: bigint | number | string | PermissionBitmask): boolean {
    const bit = PermissionBitmask.toBigIntValue(flag);
    return (this.value & bit) === bit;
  }

  public hasAnyPermission(flag: bigint | number | string | PermissionBitmask): boolean {
    const bit = PermissionBitmask.toBigIntValue(flag);
    return (this.value & bit) !== 0n;
  }

  public add(flag: bigint | number | string | PermissionBitmask): PermissionBitmask {
    const bit = PermissionBitmask.toBigIntValue(flag);
    return new PermissionBitmask(this.value | bit);
  }

  public remove(flag: bigint | number | string | PermissionBitmask): PermissionBitmask {
    const bit = PermissionBitmask.toBigIntValue(flag);
    return new PermissionBitmask(this.value & ~bit);
  }

  public combine(other: PermissionBitmask | bigint | number | string): PermissionBitmask {
    const bit = PermissionBitmask.toBigIntValue(other);
    return new PermissionBitmask(this.value | bit);
  }

  public intersect(other: PermissionBitmask | bigint | number | string): PermissionBitmask {
    const bit = PermissionBitmask.toBigIntValue(other);
    return new PermissionBitmask(this.value & bit);
  }

  public invert(): PermissionBitmask {
    return new PermissionBitmask(~this.value);
  }

  public equals(other: PermissionBitmask | bigint | number | string): boolean {
    const bit = PermissionBitmask.toBigIntValue(other);
    return this.value === bit;
  }

  public toBigInt(): bigint {
    return this.value;
  }

  public toNumber(): number {
    return Number(this.value);
  }

  public toString(): string {
    const hex = this.value.toString(16).toUpperCase().padStart(16, '0');
    return `0x${hex}`;
  }

  public static fromHex(hex: string): PermissionBitmask {
    return new PermissionBitmask(hex);
  }
}

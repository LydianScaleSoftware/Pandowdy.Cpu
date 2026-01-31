// Copyright 2026 Mark D. Long
// Licensed under the Apache License, Version 2.0
// See LICENSE file for details

namespace Pandowdy.Cpu;

/// <summary>
/// NMOS 6502 CPU with illegal opcodes treated as NOPs.
/// </summary>
public sealed class Cpu6502Simple(CpuStateBuffer buffer) : CpuBase(buffer)
{
    /// <inheritdoc />
    public override CpuVariant Variant => CpuVariant.Nmos6502Simple;

    /// <inheritdoc />
    protected override bool ClearDecimalOnInterrupt => false;
}

// Copyright 2026 Mark D. Long
// Licensed under the Apache License, Version 2.0
// See LICENSE file for details

namespace Pandowdy.Cpu;

/// <summary>
/// WDC 65C02 CPU with CMOS enhancements.
/// </summary>
public sealed class Cpu65C02(CpuStateBuffer buffer) : CpuBase(buffer)
{
    /// <inheritdoc />
    public override CpuVariant Variant => CpuVariant.Wdc65C02;

    /// <inheritdoc />
    protected override bool ClearDecimalOnInterrupt => true;
}

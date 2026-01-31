// Copyright 2026 Mark D. Long
// Licensed under the Apache License, Version 2.0
// See LICENSE file for details

using Xunit;

namespace Pandowdy.Cpu.Tests;

/// <summary>
/// Tests for interrupt handling on CPU instances.
/// </summary>
public class CpuInterruptHandlingTests
{
    private const ushort ProgramStart = 0x0400;
    private const ushort IrqHandler = 0x0500;
    private const ushort NmiHandler = 0x0600;

    [Theory]
    [InlineData(CpuVariant.Nmos6502, true)]
    [InlineData(CpuVariant.Nmos6502Simple, true)]
    [InlineData(CpuVariant.Wdc65C02, false)]
    [InlineData(CpuVariant.Rockwell65C02, false)]
    public void IrqClearsDecimalFlagForCmosVariants(CpuVariant variant, bool expectedDecimal)
    {
        var cpu = CreateCpu(variant, out var bus, out var buffer);
        buffer.Current.PC = 0x2000;
        buffer.Current.DecimalFlag = true;
        buffer.Current.InterruptDisableFlag = false;

        cpu.SignalIrq();
        bool handled = cpu.HandlePendingInterrupt(bus);

        Assert.True(handled);
        Assert.Equal(expectedDecimal, buffer.Current.DecimalFlag);
        Assert.Equal(IrqHandler, buffer.Current.PC);
        Assert.Equal(PendingInterrupt.None, buffer.Current.PendingInterrupt);
    }

    [Theory]
    [InlineData(CpuVariant.Nmos6502, true)]
    [InlineData(CpuVariant.Nmos6502Simple, true)]
    [InlineData(CpuVariant.Wdc65C02, false)]
    [InlineData(CpuVariant.Rockwell65C02, false)]
    public void NmiClearsDecimalFlagForCmosVariants(CpuVariant variant, bool expectedDecimal)
    {
        var cpu = CreateCpu(variant, out var bus, out var buffer);
        buffer.Current.PC = 0x2000;
        buffer.Current.DecimalFlag = true;

        cpu.SignalNmi();
        bool handled = cpu.HandlePendingInterrupt(bus);

        Assert.True(handled);
        Assert.Equal(expectedDecimal, buffer.Current.DecimalFlag);
        Assert.Equal(NmiHandler, buffer.Current.PC);
        Assert.Equal(PendingInterrupt.None, buffer.Current.PendingInterrupt);
    }

    [Fact]
    public void IrqIsMaskedWhenInterruptDisableSet()
    {
        var cpu = CreateCpu(CpuVariant.Nmos6502, out var bus, out var buffer);
        buffer.Current.PC = 0x2000;
        buffer.Current.InterruptDisableFlag = true;

        cpu.SignalIrq();
        bool handled = cpu.HandlePendingInterrupt(bus);

        Assert.False(handled);
        Assert.Equal(PendingInterrupt.Irq, buffer.Current.PendingInterrupt);
        Assert.Equal(0x2000, buffer.Current.PC);
    }

    [Fact]
    public void IrqWakesCpuWhenWaiting()
    {
        var cpu = CreateCpu(CpuVariant.Wdc65C02, out var bus, out var buffer);
        buffer.Current.PC = 0x2000;
        buffer.Current.InterruptDisableFlag = true;
        buffer.Current.Status = CpuStatus.Waiting;

        cpu.SignalIrq();
        bool handled = cpu.HandlePendingInterrupt(bus);

        Assert.True(handled);
        Assert.Equal(CpuStatus.Running, buffer.Current.Status);
        Assert.Equal(IrqHandler, buffer.Current.PC);
    }

    [Fact]
    public void ResetInterruptResetsRegistersAndLoadsVector()
    {
        var cpu = CreateCpu(CpuVariant.Nmos6502, out var bus, out var buffer);
        bus.SetResetVector(0x1234);
        buffer.Current.A = 0x42;
        buffer.Current.X = 0x24;
        buffer.Current.Y = 0x18;
        buffer.Current.PC = 0x2000;

        cpu.SignalReset();
        bool handled = cpu.HandlePendingInterrupt(bus);

        Assert.True(handled);
        Assert.Equal(0, buffer.Current.A);
        Assert.Equal(0, buffer.Current.X);
        Assert.Equal(0, buffer.Current.Y);
        Assert.Equal(0xFD, buffer.Current.SP);
        Assert.Equal(0x1234, buffer.Current.PC);
        Assert.Equal(CpuState.FlagU | CpuState.FlagI, buffer.Current.P);
        Assert.Equal(0, buffer.Current.CurrentOpcode);
        Assert.Equal(0, buffer.Current.OpcodeAddress);
        Assert.Equal(CpuStatus.Running, buffer.Current.Status);
        Assert.Equal(PendingInterrupt.None, buffer.Current.PendingInterrupt);
    }

    private static IPandowdyCpu CreateCpu(CpuVariant variant, out TestRamBus bus, out CpuStateBuffer buffer)
    {
        bus = new TestRamBus();
        bus.SetResetVector(ProgramStart);
        bus.SetIrqVector(IrqHandler);
        bus.SetNmiVector(NmiHandler);
        buffer = new CpuStateBuffer();
        var cpu = CpuFactory.Create(variant, buffer);
        cpu.Reset(bus);
        return cpu;
    }
}

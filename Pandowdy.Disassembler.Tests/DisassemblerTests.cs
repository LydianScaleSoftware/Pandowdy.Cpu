// Copyright 2026 Mark D. Long
// Licensed under the Apache License, Version 2.0
// See LICENSE file for details

using Pandowdy.Disassembler;
using Xunit;

using DisassemblerClass = Pandowdy.Disassembler.Disassembler;

namespace Pandowdy.Disassembler.Tests;

/// <summary>
/// Tests for the Disassembler.FormatLine method.
/// </summary>
public class DisassemblerTests
{
    #region Basic Formatting Tests

    [Fact]
    public void FormatLine_ImpliedAddressing_FormatsCorrectly()
    {
        // Arrange
        var info = new OpcodeInfo(0x18, "CLC", 0, "");
        ushort pc = 0x0800;

        // Act
        string result = DisassemblerClass.FormatLine(info, pc, 0, 0);

        // Assert
        Assert.Equal("0800: CLC", result);
    }

    [Fact]
    public void FormatLine_ImmediateAddressing_FormatsCorrectly()
    {
        // Arrange
        var info = new OpcodeInfo(0xA9, "LDA", 1, "#%1");
        ushort pc = 0x0800;
        byte p1 = 0x42;

        // Act
        string result = DisassemblerClass.FormatLine(info, pc, p1, 0);

        // Assert
        Assert.Equal("0800: LDA  #$42", result);
    }

    [Fact]
    public void FormatLine_ZeroPageAddressing_FormatsCorrectly()
    {
        // Arrange
        var info = new OpcodeInfo(0xA5, "LDA", 1, "%1");
        ushort pc = 0x0800;
        byte p1 = 0x80;

        // Act
        string result = DisassemblerClass.FormatLine(info, pc, p1, 0);

        // Assert
        Assert.Equal("0800: LDA  $80", result);
    }

    [Fact]
    public void FormatLine_AbsoluteAddressing_FormatsCorrectly()
    {
        // Arrange
        var info = new OpcodeInfo(0xAD, "LDA", 2, "%2");
        ushort pc = 0x0800;
        byte p1 = 0x00;
        byte p2 = 0xC0;

        // Act
        string result = DisassemblerClass.FormatLine(info, pc, p1, p2);

        // Assert
        Assert.Equal("0800: LDA  $C000", result);
    }

    [Fact]
    public void FormatLine_IndexedIndirect_FormatsCorrectly()
    {
        // Arrange
        var info = new OpcodeInfo(0xA1, "LDA", 1, "(%1,X)");
        ushort pc = 0x0800;
        byte p1 = 0x40;

        // Act
        string result = DisassemblerClass.FormatLine(info, pc, p1, 0);

        // Assert
        Assert.Equal("0800: LDA  ($40,X)", result);
    }

    [Fact]
    public void FormatLine_IndirectIndexed_FormatsCorrectly()
    {
        // Arrange
        var info = new OpcodeInfo(0xB1, "LDA", 1, "(%1),Y");
        ushort pc = 0x0800;
        byte p1 = 0x50;

        // Act
        string result = DisassemblerClass.FormatLine(info, pc, p1, 0);

        // Assert
        Assert.Equal("0800: LDA  ($50),Y", result);
    }

    #endregion

    #region Branch Instruction Tests

    [Fact]
    public void FormatLine_BranchForward_CalculatesTargetCorrectly()
    {
        // Arrange
        var info = new OpcodeInfo(0x10, "BPL", 1, "%branch");
        ushort pc = 0x0800;
        byte offset = 0x10; // +16 bytes

        // Act
        string result = DisassemblerClass.FormatLine(info, pc, offset, 0);

        // Assert - PC + 2 (instruction size) + 16 = $0812
        Assert.Equal("0800: BPL  $0812", result);
    }

    [Fact]
    public void FormatLine_BranchBackward_CalculatesTargetCorrectly()
    {
        // Arrange
        var info = new OpcodeInfo(0x30, "BMI", 1, "%branch");
        ushort pc = 0x0810;
        byte offset = 0xF0; // -16 bytes (signed)

        // Act
        string result = DisassemblerClass.FormatLine(info, pc, offset, 0);

        // Assert - PC + 2 - 16 = $0802
        Assert.Equal("0810: BMI  $0802", result);
    }

    [Fact]
    public void FormatLine_BranchToSelf_FormatsCorrectly()
    {
        // Arrange
        var info = new OpcodeInfo(0xF0, "BEQ", 1, "%branch");
        ushort pc = 0x0800;
        byte offset = 0xFE; // -2 (loops to itself)

        // Act
        string result = DisassemblerClass.FormatLine(info, pc, offset, 0);

        // Assert - PC + 2 - 2 = PC
        Assert.Equal("0800: BEQ  $0800", result);
    }

    [Fact]
    public void FormatLine_BranchMaxForward_HandlesCorrectly()
    {
        // Arrange
        var info = new OpcodeInfo(0xD0, "BNE", 1, "%branch");
        ushort pc = 0x0800;
        byte offset = 0x7F; // +127 (max forward)

        // Act
        string result = DisassemblerClass.FormatLine(info, pc, offset, 0);

        // Assert - PC + 2 + 127 = $0881
        Assert.Equal("0800: BNE  $0881", result);
    }

    [Fact]
    public void FormatLine_BranchMaxBackward_HandlesCorrectly()
    {
        // Arrange
        var info = new OpcodeInfo(0x90, "BCC", 1, "%branch");
        ushort pc = 0x0880;
        byte offset = 0x80; // -128 (max backward)

        // Act
        string result = DisassemblerClass.FormatLine(info, pc, offset, 0);

        // Assert - PC + 2 - 128 = $0802
        Assert.Equal("0880: BCC  $0802", result);
    }

    #endregion

    #region Undefined Opcode Tests

    [Fact]
    public void FormatLine_UndefinedOpcode_FormatsWithoutTemplate()
    {
        // Arrange
        var info = new OpcodeInfo(0x02, "???", 0, "%undef");
        ushort pc = 0x0800;

        // Act
        string result = DisassemblerClass.FormatLine(info, pc, 0, 0);

        // Assert
        Assert.Equal("0800: ???", result);
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public void FormatLine_AtAddressZero_FormatsCorrectly()
    {
        // Arrange
        var info = new OpcodeInfo(0xEA, "NOP", 0, "");
        ushort pc = 0x0000;

        // Act
        string result = DisassemblerClass.FormatLine(info, pc, 0, 0);

        // Assert
        Assert.Equal("0000: NOP", result);
    }

    [Fact]
    public void FormatLine_AtAddressFFFF_FormatsCorrectly()
    {
        // Arrange
        var info = new OpcodeInfo(0x4C, "JMP", 2, "%2");
        ushort pc = 0xFFFF;
        byte p1 = 0x00;
        byte p2 = 0xC6;

        // Act
        string result = DisassemblerClass.FormatLine(info, pc, p1, p2);

        // Assert
        Assert.Equal("FFFF: JMP  $C600", result);
    }

    [Fact]
    public void FormatLine_BranchAcrossBoundary_HandlesWrapAround()
    {
        // Arrange
        var info = new OpcodeInfo(0x10, "BPL", 1, "%branch");
        ushort pc = 0xFFFE;
        byte offset = 0x05; // Should wrap to $0005

        // Act
        string result = DisassemblerClass.FormatLine(info, pc, offset, 0);

        // Assert - FFFE + 2 + 5 = 0005 (with wrap)
        Assert.Equal("FFFE: BPL  $0005", result);
    }

    #endregion

    #region Complex Addressing Mode Tests

    [Fact]
    public void FormatLine_ZeroPageX_FormatsCorrectly()
    {
        // Arrange
        var info = new OpcodeInfo(0xB5, "LDA", 1, "%1,X");
        ushort pc = 0x0800;
        byte p1 = 0x20;

        // Act
        string result = DisassemblerClass.FormatLine(info, pc, p1, 0);

        // Assert
        Assert.Equal("0800: LDA  $20,X", result);
    }

    [Fact]
    public void FormatLine_AbsoluteX_FormatsCorrectly()
    {
        // Arrange
        var info = new OpcodeInfo(0xBD, "LDA", 2, "%2,X");
        ushort pc = 0x0800;
        byte p1 = 0x00;
        byte p2 = 0x20;

        // Act
        string result = DisassemblerClass.FormatLine(info, pc, p1, p2);

        // Assert
        Assert.Equal("0800: LDA  $2000,X", result);
    }

    [Fact]
    public void FormatLine_AbsoluteY_FormatsCorrectly()
    {
        // Arrange
        var info = new OpcodeInfo(0xB9, "LDA", 2, "%2,Y");
        ushort pc = 0x0800;
        byte p1 = 0x00;
        byte p2 = 0x40;

        // Act
        string result = DisassemblerClass.FormatLine(info, pc, p1, p2);

        // Assert
        Assert.Equal("0800: LDA  $4000,Y", result);
    }

    [Fact]
    public void FormatLine_IndirectJump_FormatsCorrectly()
    {
        // Arrange
        var info = new OpcodeInfo(0x6C, "JMP", 2, "(%2)");
        ushort pc = 0x0800;
        byte p1 = 0xFC;
        byte p2 = 0xFF;

        // Act
        string result = DisassemblerClass.FormatLine(info, pc, p1, p2);

        // Assert
        Assert.Equal("0800: JMP  ($FFFC)", result);
    }

    #endregion

    #region Mnemonic Alignment Tests

    [Fact]
    public void FormatLine_ShortMnemonic_PadsCorrectly()
    {
        // Arrange - 3-letter mnemonic
        var info = new OpcodeInfo(0xA9, "LDA", 1, "#%1");
        ushort pc = 0x0800;

        // Act
        string result = DisassemblerClass.FormatLine(info, pc, 0x42, 0);

        // Assert - Should have 4-character width (3 letters + 1 space)
        Assert.Contains("LDA ", result);
        Assert.Equal("0800: LDA  #$42", result);
    }

    [Fact]
    public void FormatLine_UndefinedMnemonic_PadsCorrectly()
    {
        // Arrange - 3-letter mnemonic
        var info = new OpcodeInfo(0x02, "???", 0, "%undef");
        ushort pc = 0x0800;

        // Act
        string result = DisassemblerClass.FormatLine(info, pc, 0, 0);

        // Assert
        Assert.Equal("0800: ???", result);
    }

    #endregion
}


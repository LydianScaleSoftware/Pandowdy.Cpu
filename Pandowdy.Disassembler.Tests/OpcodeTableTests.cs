// Copyright 2026 Mark D. Long
// Licensed under the Apache License, Version 2.0
// See LICENSE file for details

using Pandowdy.Disassembler;
using Xunit;

namespace Pandowdy.Disassembler.Tests;

/// <summary>
/// Tests for the OpcodeTable to ensure all 256 opcodes are correctly defined.
/// </summary>
public class OpcodeTableTests
{
    #region Table Structure Tests

    [Fact]
    public void Table_Has256Entries()
    {
        // Assert
        Assert.Equal(256, OpcodeTable.Table.Length);
    }

    [Fact]
    public void Table_AllEntriesAreInitialized()
    {
        // Act & Assert
        for (int i = 0; i < 256; i++)
        {
            var entry = OpcodeTable.Table[i];
            Assert.NotNull(entry.Mnemonic);
            Assert.NotNull(entry.Template);
        }
    }

    [Fact]
    public void Table_OpcodeValuesMatchIndices()
    {
        // Act & Assert - Each entry's Opcode field should match its index
        for (int i = 0; i < 256; i++)
        {
            Assert.Equal((byte)i, OpcodeTable.Table[i].Opcode);
        }
    }

    #endregion

    #region Common Opcode Tests

    [Fact]
    public void Table_LDA_Immediate_IsCorrect()
    {
        // Arrange & Act
        var info = OpcodeTable.Table[0xA9];

        // Assert
        Assert.Equal(0xA9, info.Opcode);
        Assert.Equal("LDA", info.Mnemonic);
        Assert.Equal(1, info.ParamBytes);
        Assert.Equal("#%1", info.Template);
    }

    [Fact]
    public void Table_STA_Absolute_IsCorrect()
    {
        // Arrange & Act
        var info = OpcodeTable.Table[0x8D];

        // Assert
        Assert.Equal(0x8D, info.Opcode);
        Assert.Equal("STA", info.Mnemonic);
        Assert.Equal(2, info.ParamBytes);
        Assert.Equal("%2", info.Template);
    }

    [Fact]
    public void Table_JMP_Absolute_IsCorrect()
    {
        // Arrange & Act
        var info = OpcodeTable.Table[0x4C];

        // Assert
        Assert.Equal(0x4C, info.Opcode);
        Assert.Equal("JMP", info.Mnemonic);
        Assert.Equal(2, info.ParamBytes);
        Assert.Equal("%2", info.Template);
    }

    [Fact]
    public void Table_JSR_IsCorrect()
    {
        // Arrange & Act
        var info = OpcodeTable.Table[0x20];

        // Assert
        Assert.Equal(0x20, info.Opcode);
        Assert.Equal("JSR", info.Mnemonic);
        Assert.Equal(2, info.ParamBytes);
        Assert.Equal("%2", info.Template);
    }

    [Fact]
    public void Table_RTS_IsCorrect()
    {
        // Arrange & Act
        var info = OpcodeTable.Table[0x60];

        // Assert
        Assert.Equal(0x60, info.Opcode);
        Assert.Equal("RTS", info.Mnemonic);
        Assert.Equal(0, info.ParamBytes);
        Assert.Equal("", info.Template);
    }

    [Fact]
    public void Table_NOP_IsCorrect()
    {
        // Arrange & Act
        var info = OpcodeTable.Table[0xEA];

        // Assert
        Assert.Equal(0xEA, info.Opcode);
        Assert.Equal("NOP", info.Mnemonic);
        Assert.Equal(0, info.ParamBytes);
        Assert.Equal("", info.Template);
    }

    #endregion

    #region Branch Instruction Tests

    [Theory]
    [InlineData(0x10, "BPL")]  // Branch on PLus
    [InlineData(0x30, "BMI")]  // Branch on MInus
    [InlineData(0x50, "BVC")]  // Branch on oVerflow Clear
    [InlineData(0x70, "BVS")]  // Branch on oVerflow Set
    [InlineData(0x90, "BCC")]  // Branch on Carry Clear
    [InlineData(0xB0, "BCS")]  // Branch on Carry Set
    [InlineData(0xD0, "BNE")]  // Branch on Not Equal
    [InlineData(0xF0, "BEQ")]  // Branch on EQual
    public void Table_BranchInstructions_HaveBranchTemplate(byte opcode, string expectedMnemonic)
    {
        // Arrange & Act
        var info = OpcodeTable.Table[opcode];

        // Assert
        Assert.Equal(expectedMnemonic, info.Mnemonic);
        Assert.Equal(1, info.ParamBytes);
        Assert.Equal("%branch", info.Template);
    }

    #endregion

    #region 65C02 New Instructions Tests

    [Theory]
    [InlineData(0x1A, "INC", 0, "")]      // INC A
    [InlineData(0x3A, "DEC", 0, "")]      // DEC A
    [InlineData(0x04, "TSB", 1, "%1")]    // Test and Set Bits (ZP)
    [InlineData(0x0C, "TSB", 2, "%2")]    // Test and Set Bits (Abs)
    [InlineData(0x14, "TRB", 1, "%1")]    // Test and Reset Bits (ZP)
    [InlineData(0x1C, "TRB", 2, "%2")]    // Test and Reset Bits (Abs)
    public void Table_65C02NewInstructions_AreCorrect(byte opcode, string mnemonic, byte paramBytes, string template)
    {
        // Arrange & Act
        var info = OpcodeTable.Table[opcode];

        // Assert
        Assert.Equal(mnemonic, info.Mnemonic);
        Assert.Equal(paramBytes, info.ParamBytes);
        Assert.Equal(template, info.Template);
    }

    #endregion

    #region Addressing Mode Tests

    [Theory]
    [InlineData(0xA5, "LDA", 1, "%1")]          // Zero Page
    [InlineData(0xB5, "LDA", 1, "%1,X")]        // Zero Page,X
    [InlineData(0xB6, "LDX", 1, "%1,Y")]        // Zero Page,Y
    [InlineData(0xAD, "LDA", 2, "%2")]          // Absolute
    [InlineData(0xBD, "LDA", 2, "%2,X")]        // Absolute,X
    [InlineData(0xB9, "LDA", 2, "%2,Y")]        // Absolute,Y
    [InlineData(0xA1, "LDA", 1, "(%1,X)")]      // Indexed Indirect
    [InlineData(0xB1, "LDA", 1, "(%1),Y")]      // Indirect Indexed
    [InlineData(0xB2, "LDA", 1, "(%1)")]        // Indirect (65C02 Rockwell)
    public void Table_AddressingModes_AreCorrect(byte opcode, string mnemonic, byte paramBytes, string template)
    {
        // Arrange & Act
        var info = OpcodeTable.Table[opcode];

        // Assert
        Assert.Equal(mnemonic, info.Mnemonic);
        Assert.Equal(paramBytes, info.ParamBytes);
        Assert.Equal(template, info.Template);
    }

    #endregion

    #region Undefined Opcode Tests

    [Theory]
    [InlineData(0x02)]
    [InlineData(0x03)]
    [InlineData(0x07)]
    [InlineData(0x0B)]
    [InlineData(0x0F)]
    [InlineData(0x13)]
    [InlineData(0x17)]
    [InlineData(0x1B)]
    [InlineData(0x1F)]
    public void Table_UndefinedOpcodes_HaveUndefTemplate(byte opcode)
    {
        // Arrange & Act
        var info = OpcodeTable.Table[opcode];

        // Assert
        Assert.Equal("???", info.Mnemonic);
        Assert.Equal("%undef", info.Template);
    }

    #endregion

    #region Implied/Accumulator Instructions Tests

    [Theory]
    [InlineData(0x18, "CLC")]  // CLear Carry
    [InlineData(0x38, "SEC")]  // SEt Carry
    [InlineData(0x58, "CLI")]  // CLear Interrupt
    [InlineData(0x78, "SEI")]  // SEt Interrupt
    [InlineData(0xB8, "CLV")]  // CLear oVerflow
    [InlineData(0xD8, "CLD")]  // CLear Decimal
    [InlineData(0xF8, "SED")]  // SEt Decimal
    [InlineData(0x0A, "ASL")]  // Arithmetic Shift Left (A)
    [InlineData(0x2A, "ROL")]  // ROtate Left (A)
    [InlineData(0x4A, "LSR")]  // Logical Shift Right (A)
    [InlineData(0x6A, "ROR")]  // ROtate Right (A)
    public void Table_ImpliedInstructions_HaveNoParams(byte opcode, string mnemonic)
    {
        // Arrange & Act
        var info = OpcodeTable.Table[opcode];

        // Assert
        Assert.Equal(mnemonic, info.Mnemonic);
        Assert.Equal(0, info.ParamBytes);
        Assert.Equal("", info.Template);
    }

    #endregion

    #region Stack Instructions Tests

    [Theory]
    [InlineData(0x48, "PHA")]  // PusH Accumulator
    [InlineData(0x68, "PLA")]  // PuLl Accumulator
    [InlineData(0x08, "PHP")]  // PusH Processor status
    [InlineData(0x28, "PLP")]  // PuLl Processor status
    public void Table_StackInstructions_AreImplied(byte opcode, string mnemonic)
    {
        // Arrange & Act
        var info = OpcodeTable.Table[opcode];

        // Assert
        Assert.Equal(mnemonic, info.Mnemonic);
        Assert.Equal(0, info.ParamBytes);
        Assert.Equal("", info.Template);
    }

    #endregion

    #region Transfer Instructions Tests

    [Theory]
    [InlineData(0xAA, "TAX")]  // Transfer A to X
    [InlineData(0x8A, "TXA")]  // Transfer X to A
    [InlineData(0xA8, "TAY")]  // Transfer A to Y
    [InlineData(0x98, "TYA")]  // Transfer Y to A
    [InlineData(0xBA, "TSX")]  // Transfer SP to X
    [InlineData(0x9A, "TXS")]  // Transfer X to SP
    public void Table_TransferInstructions_AreImplied(byte opcode, string mnemonic)
    {
        // Arrange & Act
        var info = OpcodeTable.Table[opcode];

        // Assert
        Assert.Equal(mnemonic, info.Mnemonic);
        Assert.Equal(0, info.ParamBytes);
        Assert.Equal("", info.Template);
    }

    #endregion

    #region Special Instructions Tests

    [Fact]
    public void Table_BRK_HasCorrectParams()
    {
        // Arrange & Act
        var info = OpcodeTable.Table[0x00];

        // Assert
        Assert.Equal("BRK", info.Mnemonic);
        Assert.Equal(1, info.ParamBytes);  // BRK pushes PC+2
        Assert.Equal("%1", info.Template);
    }

    [Fact]
    public void Table_RTI_IsImplied()
    {
        // Arrange & Act
        var info = OpcodeTable.Table[0x40];

        // Assert
        Assert.Equal("RTI", info.Mnemonic);
        Assert.Equal(0, info.ParamBytes);
        Assert.Equal("", info.Template);
    }

    [Fact]
    public void Table_JMP_Indirect_HasCorrectTemplate()
    {
        // Arrange & Act
        var info = OpcodeTable.Table[0x6C];

        // Assert
        Assert.Equal("JMP", info.Mnemonic);
        Assert.Equal(2, info.ParamBytes);
        Assert.Equal("(%2)", info.Template);
    }

    #endregion

    #region Rockwell/WDC Extensions Tests

    [Theory]
    [InlineData(0x47, "RMB0", 1, "%1")]  // Reset Memory Bit 0
    [InlineData(0x57, "RMB1", 1, "%1")]
    [InlineData(0x67, "RMB2", 1, "%1")]
    [InlineData(0x77, "RMB3", 1, "%1")]
    [InlineData(0x87, "SMB0", 1, "%1")]  // Set Memory Bit 0
    [InlineData(0x97, "SMB1", 1, "%1")]
    [InlineData(0xA7, "SMB2", 1, "%1")]
    [InlineData(0xB7, "SMB3", 1, "%1")]
    public void Table_RockwellBitOperations_AreCorrect(byte opcode, string mnemonic, byte paramBytes, string template)
    {
        // Arrange & Act
        var info = OpcodeTable.Table[opcode];

        // Assert
        Assert.Equal(mnemonic, info.Mnemonic);
        Assert.Equal(paramBytes, info.ParamBytes);
        Assert.Equal(template, info.Template);
    }

    #endregion

    #region Consistency Tests

    [Fact]
    public void Table_AllOpcodes_HaveNonEmptyMnemonic()
    {
        // Act & Assert
        for (int i = 0; i < 256; i++)
        {
            var info = OpcodeTable.Table[i];
            Assert.False(string.IsNullOrWhiteSpace(info.Mnemonic),
                $"Opcode ${i:X2} has empty mnemonic");
        }
    }

    [Fact]
    public void Table_ParamBytes_IsValidRange()
    {
        // Act & Assert
        for (int i = 0; i < 256; i++)
        {
            var info = OpcodeTable.Table[i];
            Assert.InRange(info.ParamBytes, 0, 2);
        }
    }

    [Fact]
    public void Table_AllOpcodes_HaveNonNullTemplate()
    {
        // Act & Assert
        for (int i = 0; i < 256; i++)
        {
            var info = OpcodeTable.Table[i];
            Assert.NotNull(info.Template);
        }
    }

    #endregion
}

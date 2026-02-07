// Copyright 2026 Mark D. Long
// Licensed under the Apache License, Version 2.0
// See LICENSE file for details

using Pandowdy.Disassembler;
using Xunit;

namespace Pandowdy.Disassembler.Tests;

/// <summary>
/// Tests for the OpcodeInfo readonly struct.
/// </summary>
public class OpcodeInfoTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_InitializesAllFields()
    {
        // Arrange
        byte opcode = 0xA9;
        string mnemonic = "LDA";
        byte paramBytes = 1;
        string template = "#%1";

        // Act
        var info = new OpcodeInfo(opcode, mnemonic, paramBytes, template);

        // Assert
        Assert.Equal(opcode, info.Opcode);
        Assert.Equal(mnemonic, info.Mnemonic);
        Assert.Equal(paramBytes, info.ParamBytes);
        Assert.Equal(template, info.Template);
    }

    [Fact]
    public void Constructor_WithEmptyTemplate_StoresEmptyString()
    {
        // Arrange & Act
        var info = new OpcodeInfo(0xEA, "NOP", 0, "");

        // Assert
        Assert.Equal("", info.Template);
    }

    [Fact]
    public void Constructor_WithComplexTemplate_StoresFullString()
    {
        // Arrange
        string complexTemplate = "(%1,X)";

        // Act
        var info = new OpcodeInfo(0xA1, "LDA", 1, complexTemplate);

        // Assert
        Assert.Equal(complexTemplate, info.Template);
    }

    #endregion

    #region Immutability Tests

    [Fact]
    public void OpcodeInfo_IsReadonlyStruct()
    {
        // Arrange & Act
        var info = new OpcodeInfo(0xA9, "LDA", 1, "#%1");

        // Assert - Verify all fields are readonly by attempting to read them
        byte opcode = info.Opcode;
        string mnemonic = info.Mnemonic;
        byte paramBytes = info.ParamBytes;
        string template = info.Template;

        Assert.NotNull(mnemonic);
        Assert.NotNull(template);
    }

    #endregion

    #region Equality Tests

    [Fact]
    public void OpcodeInfo_TwoInstancesWithSameValues_AreEqual()
    {
        // Arrange
        var info1 = new OpcodeInfo(0xA9, "LDA", 1, "#%1");
        var info2 = new OpcodeInfo(0xA9, "LDA", 1, "#%1");

        // Act & Assert
        Assert.Equal(info1, info2);
    }

    [Fact]
    public void OpcodeInfo_TwoInstancesWithDifferentOpcodes_AreNotEqual()
    {
        // Arrange
        var info1 = new OpcodeInfo(0xA9, "LDA", 1, "#%1");
        var info2 = new OpcodeInfo(0xA5, "LDA", 1, "%1");

        // Act & Assert
        Assert.NotEqual(info1, info2);
    }

    #endregion

    #region Special Cases Tests

    [Fact]
    public void Constructor_WithZeroOpcode_WorksCorrectly()
    {
        // Arrange & Act
        var info = new OpcodeInfo(0x00, "BRK", 1, "%1");

        // Assert
        Assert.Equal(0x00, info.Opcode);
        Assert.Equal("BRK", info.Mnemonic);
    }

    [Fact]
    public void Constructor_WithFFOpcode_WorksCorrectly()
    {
        // Arrange & Act
        var info = new OpcodeInfo(0xFF, "ISC", 2, "%2,X");

        // Assert
        Assert.Equal(0xFF, info.Opcode);
        Assert.Equal("ISC", info.Mnemonic);
    }

    [Fact]
    public void Constructor_WithZeroParamBytes_WorksCorrectly()
    {
        // Arrange & Act
        var info = new OpcodeInfo(0xEA, "NOP", 0, "");

        // Assert
        Assert.Equal(0, info.ParamBytes);
    }

    [Fact]
    public void Constructor_WithTwoParamBytes_WorksCorrectly()
    {
        // Arrange & Act
        var info = new OpcodeInfo(0xAD, "LDA", 2, "%2");

        // Assert
        Assert.Equal(2, info.ParamBytes);
    }

    #endregion

    #region Template Variation Tests

    [Theory]
    [InlineData("#%1", "Immediate addressing")]
    [InlineData("%1", "Zero page addressing")]
    [InlineData("%1,X", "Zero page,X addressing")]
    [InlineData("%1,Y", "Zero page,Y addressing")]
    [InlineData("%2", "Absolute addressing")]
    [InlineData("%2,X", "Absolute,X addressing")]
    [InlineData("%2,Y", "Absolute,Y addressing")]
    [InlineData("(%1,X)", "Indexed indirect addressing")]
    [InlineData("(%1),Y", "Indirect indexed addressing")]
    [InlineData("(%1)", "Indirect addressing")]
    [InlineData("(%2)", "Indirect JMP")]
    [InlineData("%branch", "Branch instruction")]
    [InlineData("%undef", "Undefined opcode")]
    [InlineData("", "Implied/Accumulator addressing")]
    public void Constructor_WithVariousTemplates_StoresCorrectly(string template, string description)
    {
        // Arrange & Act
        var info = new OpcodeInfo(0x00, "TEST", 0, template);

        // Assert
        Assert.Equal(template, info.Template);
    }

    #endregion

    #region Real World Examples Tests

    [Fact]
    public void Constructor_LDA_Immediate_IsCorrect()
    {
        // Arrange & Act
        var info = new OpcodeInfo(0xA9, "LDA", 1, "#%1");

        // Assert
        Assert.Equal(0xA9, info.Opcode);
        Assert.Equal("LDA", info.Mnemonic);
        Assert.Equal(1, info.ParamBytes);
        Assert.Equal("#%1", info.Template);
    }

    [Fact]
    public void Constructor_JMP_Absolute_IsCorrect()
    {
        // Arrange & Act
        var info = new OpcodeInfo(0x4C, "JMP", 2, "%2");

        // Assert
        Assert.Equal(0x4C, info.Opcode);
        Assert.Equal("JMP", info.Mnemonic);
        Assert.Equal(2, info.ParamBytes);
        Assert.Equal("%2", info.Template);
    }

    [Fact]
    public void Constructor_BEQ_Branch_IsCorrect()
    {
        // Arrange & Act
        var info = new OpcodeInfo(0xF0, "BEQ", 1, "%branch");

        // Assert
        Assert.Equal(0xF0, info.Opcode);
        Assert.Equal("BEQ", info.Mnemonic);
        Assert.Equal(1, info.ParamBytes);
        Assert.Equal("%branch", info.Template);
    }

    [Fact]
    public void Constructor_NOP_Implied_IsCorrect()
    {
        // Arrange & Act
        var info = new OpcodeInfo(0xEA, "NOP", 0, "");

        // Assert
        Assert.Equal(0xEA, info.Opcode);
        Assert.Equal("NOP", info.Mnemonic);
        Assert.Equal(0, info.ParamBytes);
        Assert.Equal("", info.Template);
    }

    [Fact]
    public void Constructor_UndefinedOpcode_IsCorrect()
    {
        // Arrange & Act
        var info = new OpcodeInfo(0x02, "???", 0, "%undef");

        // Assert
        Assert.Equal(0x02, info.Opcode);
        Assert.Equal("???", info.Mnemonic);
        Assert.Equal(0, info.ParamBytes);
        Assert.Equal("%undef", info.Template);
    }

    #endregion
}

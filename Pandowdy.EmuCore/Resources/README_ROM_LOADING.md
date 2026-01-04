# SystemRomProvider Resource Loading

## Overview

The `SystemRomProvider` class supports loading ROM data from both the file system and embedded resources using a simple prefix convention.

## ROM Size and Layout

The `SystemRomProvider` manages the **16KB system ROM** spanning **$C000-$FFFF**:

```
$C000-$C0FF (256 bytes)  - I/O space firmware
$C100-$C7FF (1792 bytes) - Internal peripheral ROM (7 x 256 bytes)
$C800-$CFFF (2KB)        - Extended internal ROM
$D000-$DFFF (4KB)        - Monitor ROM / Language Card bank area
$E000-$FFFF (8KB)        - Applesoft BASIC + reset vector / Language Card common area
```

### Address Mapping

The ROM provider's internal array starts at offset 0, representing $C000:

| Apple IIe Address | ROM Provider Offset | Description |
|-------------------|---------------------|-------------|
| `$C000` | `0x0000` | I/O firmware start |
| `$D000` | `0x1000` | Monitor ROM / Language Card bank start |
| `$E000` | `0x2000` | BASIC ROM / Language Card common start |
| `$FFFC-$FFFD` | `0x3FFC-0x3FFD` | Reset vector |
| `$FFFF` | `0x3FFF` | End of ROM |

## Loading Sources

### File System (Traditional)

Load ROM from a file on disk:

```csharp
var rom = new SystemRomProvider("roms/AppleIIe.rom");
```

### Embedded Resource (New)

Load ROM from an embedded resource using the `res:` prefix:

```csharp
var rom = new SystemRomProvider("res:Pandowdy.EmuCore.Resources.a2e_enh_c-f.rom");
```

## Resource Naming Convention

When using the `res:` prefix:

1. **Prefix:** Start with `"res:"` (case-insensitive)
2. **Resource Name:** Follow with the fully-qualified resource name
3. **Format:** `"res:<Namespace>.<Path>.<Filename>"`

### Example

For a resource at `Pandowdy.EmuCore\Resources\a2e_enh_c-f.rom` embedded with default namespace:

```
File Path:     assets\a2e_enh_c-f.rom
Resource Name: Pandowdy.EmuCore.Resources.a2e_enh_c-f.rom
Usage:         "res:Pandowdy.EmuCore.Resources.a2e_enh_c-f.rom"
```

## Embedded Resources in Project

To embed a ROM resource in your project:

```xml
<ItemGroup>
  <EmbeddedResource Include="..\assets\a2e_enh_c-f.rom">
    <Link>Resources\a2e_enh_c-f.rom</Link>
  </EmbeddedResource>
</ItemGroup>
```

## ROM Size Validation

The `SystemRomProvider` expects exactly **16KB (16,384 bytes / 0x4000)** of ROM data.

This corresponds to the full Apple IIe system ROM:
- `$C000-$CFFF` (4KB) - I/O firmware and peripheral ROMs
- `$D000-$FFFF` (12KB) - Monitor ROM and BASIC ROM (accessible via Language Card)

### Why 16KB?

The SystemRomProvider provides the **entire** ROM space, not just the Language Card portion:

- **$C000-$CFFF**: Used by the system bus for I/O firmware and expansion slot ROMs
- **$D000-$FFFF**: Accessible through the Language Card for Monitor and BASIC

The Language Card component reads from offsets `0x1000-0x3FFF` within this 16KB ROM.

## Usage Examples

### Basic Usage

```csharp
// Load from file
var fileRom = new SystemRomProvider("AppleIIe.rom");

// Load from embedded resource
var resourceRom = new SystemRomProvider("res:Pandowdy.EmuCore.Resources.a2e_enh_c-f.rom");

// Access Monitor ROM at $D000 (offset 0x1000)
byte monitorByte = fileRom.Read(0x1000);

// Access reset vector at $FFFC (offset 0x3FFC)
byte resetLo = fileRom.Read(0x3FFC);
byte resetHi = fileRom.Read(0x3FFD);
ushort resetVector = (ushort)(resetLo | (resetHi << 8));
```

### With Language Card

```csharp
var rom = new SystemRomProvider("res:Pandowdy.EmuCore.Resources.a2e_enh_c-f.rom");

// Language Card reads from $D000-$FFFF, which maps to ROM offsets 0x1000-0x3FFF
var languageCard = new LanguageCard(mainRam, auxRam, rom, floatingBus, status);

// When reading $D000, Language Card internally reads rom[0x1000]
byte value = languageCard.Read(0xD000);
```

### With Dependency Injection

```csharp
services.AddSingleton<ISystemRomProvider>(sp => 
    new SystemRomProvider("res:Pandowdy.EmuCore.Resources.a2e_enh_c-f.rom"));
```

### Runtime Selection

```csharp
public ISystemRomProvider CreateRomProvider(string romSource)
{
    // Can be file path or resource identifier
    return new SystemRomProvider(romSource);
}

// Usage:
var rom1 = CreateRomProvider("custom_roms/modified.rom");
var rom2 = CreateRomProvider("res:MyApp.Resources.stock.rom");
```

## Error Handling

### File Not Found

```csharp
try
{
    var rom = new SystemRomProvider("missing.rom");
}
catch (FileNotFoundException ex)
{
    // File doesn't exist on disk
}
```

### Resource Not Found

```csharp
try
{
    var rom = new SystemRomProvider("res:MyApp.Resources.Missing.rom");
}
catch (FileNotFoundException ex)
{
    // Resource not found in assembly
    // Check: 1. Resource is embedded, 2. Name is correct
}
```

### Invalid Size

```csharp
try
{
    var rom = new SystemRomProvider("wrong_size.rom");
}
catch (InvalidDataException ex)
{
    // ROM is not exactly 16KB
    Console.WriteLine($"Expected 16KB ROM, got {ex.Message}");
}
```

## Available Embedded ROMs

### Pandowdy.EmuCore

| Resource Name | Description | Size |
|---------------|-------------|------|
| `Pandowdy.EmuCore.Resources.a2e_enh_c-f.rom` | Apple IIe Enhanced ROM (full 16KB, $C000-$FFFF) | 16KB |
| `Pandowdy.EmuCore.Resources.a2e_enh_video.rom` | Video ROM (character generator) | Varies |

## Benefits of Resource Loading

### Advantages

✅ **No External Files** - ROM embedded in executable  
✅ **Deployment Simplicity** - Single .exe/.dll file  
✅ **Version Control** - ROM is part of source code  
✅ **Reliability** - No file-not-found at runtime  
✅ **Distribution** - No separate ROM file downloads  

### When to Use Files vs Resources

| Use Case | Recommended |
|----------|-------------|
| **Standard distribution** | Embedded Resource (`res:`) |
| **Custom ROMs** | File System |
| **Testing different ROMs** | File System |
| **User-provided ROMs** | File System |
| **Open source projects** | Embedded Resource |
| **Commercial products** | Embedded Resource |

## Migration Guide

### From Old VA2M.TryLoadEmbeddedRom()

**Old Code:**
```csharp
TryLoadEmbeddedRom("Pandowdy.EmuCore.Resources.a2e_enh_c-f.rom");
```

**New Code:**
```csharp
var rom = new SystemRomProvider("res:Pandowdy.EmuCore.Resources.a2e_enh_c-f.rom");
// ROM now accessible through ISystemRomProvider interface
```

### From External ROM File

**Old Code:**
```csharp
byte[] romData = File.ReadAllBytes("AppleIIe.rom");
```

**New Code:**
```csharp
var rom = new SystemRomProvider("AppleIIe.rom");
// Or use embedded: "res:MyApp.Resources.AppleIIe.rom"
```

## Technical Implementation

### Detection Logic

```csharp
if (filename.StartsWith("res:", StringComparison.OrdinalIgnoreCase))
{
    // Load from embedded resource
    LoadFromResource(resourceName);
}
else
{
    // Load from file system
    LoadFromFile(filename);
}
```

### Resource Loading

```csharp
var assembly = Assembly.GetExecutingAssembly();
using var stream = assembly.GetManifestResourceStream(resourceName);
// Read stream into byte array
```

## Testing

The `SystemRomProviderTests` class includes comprehensive tests for both file and resource loading:

```csharp
[Fact]
public void Constructor_WithResourcePrefix_LoadsFromResource()
{
    var rom = new SystemRomProvider("res:Pandowdy.EmuCore.Resources.a2e_enh_c-f.rom");
    Assert.NotNull(rom);
    Assert.Equal(0x4000, rom.Size); // 16KB
}
```

## See Also

- [ISystemRomProvider Interface](../Interfaces/ISystemRomProvider.cs)
- [SystemRomProvider Implementation](../SystemRomProvider.cs)
- [LanguageCard ROM Mapping](../LanguageCard.cs)
- [MemoryPool ROM Installation](../MemoryPool.cs)
- [SystemRomProviderTests](../../Pandowdy.EmuCore.Tests/SystemRomProviderTests.cs)

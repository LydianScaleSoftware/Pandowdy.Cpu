# Pandowdy - Copilot Workspace Instructions

This file provides guidance for AI assistants working in the Pandowdy Apple IIe emulator codebase.

## Git Best Practices

### File Operations
- **ALWAYS use `git mv`** when moving or renaming files to preserve Git history
- **Never use create/delete cycles** for file moves - this loses history
- Check for `.git` directory before file operations
- Prefer Git-aware commands in version-controlled workspaces

### Examples
```bash
# Correct: Preserves history
git mv old/path/File.cs new/path/File.cs

# Incorrect: Loses history
# create_file(new/path/File.cs)
# remove_file(old/path/File.cs)
```

## Project Structure

### Pandowdy.EmuCore (Emulator Core)
```
Pandowdy.EmuCore/
??? Root (Core Domain)
?   ??? VA2M.cs                    - Main emulator orchestrator
?   ??? VA2MBus.cs                 - Central bus coordinator (~570 lines, well-tested)
?   ??? MemoryPool.cs              - 128KB Apple IIe memory model
?   ??? CPUAdapter.cs              - Adapter for 6502.NET CPU
?   ??? SoftSwitch.cs              - Apple II soft switch model
?   ??? BitField16.cs              - Utility data structure
?   ??? BitmapDataArray.cs         - Core bitmap data type
?   ??? RenderContext.cs           - Rendering data structure
?
??? Services/ (Cross-cutting Services)
    ??? EmulatorStateServices.cs   - State management (EmulatorStateProvider)
    ??? SystemStatusServices.cs    - Status provider (SystemStatusProvider + ISoftSwitchResponder)
    ??? FrameProvider.cs           - Double-buffered frame management
    ??? FrameGenerator.cs          - Frame generation coordinator
    ??? CharacterRomProvider.cs    - Apple IIe character ROM provider
    ??? LegacyBitmapRenderer.cs    - Bitmap rendering service
```

### Pandowdy.Tests (Test Mirror)
```
Pandowdy.Tests/
??? Root (Core Domain Tests)
?   ??? VA2MTests.cs
?   ??? VA2MBusTests.cs            - 80+ comprehensive tests
?   ??? MemoryPoolTests.cs
?   ??? CPUAdapterTests.cs
?   ??? SoftSwitchTests.cs
?   ??? BitField16Tests.cs
?   ??? BitmapDataArrayTests.cs
?   ??? RenderingIntegrationTests.cs
?
??? Services/ (Service Tests - mirrors EmuCore/Services)
    ??? EmulatorStateProviderTests.cs
    ??? SystemStatusProviderTests.cs
    ??? FrameProviderTests.cs
    ??? FrameGeneratorTests.cs
    ??? CharacterRomProviderTests.cs
    ??? LegacyBitmapRendererTests.cs
```

### Architecture Principles
- **Core Domain** (root): Direct hardware representations, orchestrators, data structures
- **Services** (Services/): Providers, coordinators, cross-cutting concerns
- **Tests Mirror Production**: Test structure exactly matches production code structure
- **Dependency Injection**: Services registered in `Pandowdy/Program.cs`

## Code Standards

### Technology Stack
- **Target Framework**: .NET 8
- **Language**: C# 12.0
- **UI Framework**: Avalonia (cross-platform)
- **Testing**: xUnit
- **Reactive Extensions**: System.Reactive (Rx.NET)
- **Legacy CPU**: 6502.NET library (submodule)

### Coding Conventions
- Follow existing patterns in the codebase
- Services use constructor injection
- Use `ArgumentNullException.ThrowIfNull()` for null checks
- Immutable snapshots for state (records with `with` expressions)
- Async/await for I/O operations
- Events for cross-component communication

### Namespace Conventions
```csharp
// Production code
namespace Pandowdy.EmuCore;                    // Core domain
namespace Pandowdy.EmuCore.Services;           // Services
namespace Pandowdy.EmuCore.Interfaces;         // Interfaces

// Test code
namespace Pandowdy.Tests;                      // Core tests
namespace Pandowdy.Tests.Services;             // Service tests
```

## Testing Requirements

### Test Coverage
- **Current test count**: 553+ tests
- **Pass rate**: 100% required
- All changes must pass existing tests
- New services require comprehensive test coverage
- Test file naming: `{ClassName}Tests.cs`

### Test Organization
- Place tests in same structure as production code
- Service tests go in `Pandowdy.Tests/Services/`
- Use test fixtures for complex setup
- Group tests by functionality with `#region` blocks

### Test Examples
```csharp
// VA2MBusTests.cs has excellent patterns:
// - Comprehensive coverage (80+ tests)
// - Clear test regions (#region Constructor Tests, etc.)
// - Helper fixture classes
// - Integration scenarios
// - Edge case testing
```

## Apple II Emulation Domain

### Key Components
- **VA2M**: Top-level emulator class, orchestrates all subsystems
- **VA2MBus**: Central bus coordinator, handles all I/O operations ($C000-$CFFF)
- **MemoryPool**: 128KB memory model (64KB main + 64KB aux)
- **Soft Switches**: Apple IIe memory/video mode configuration ($C000-$C05F)
- **Language Card**: Bank switching ($C080-$C08F)
- **Frame Generation Pipeline**: FrameProvider ? FrameGenerator ? Renderer

### Memory Map (Apple IIe Enhanced)
- `$0000-$01FF`: Zero page & stack
- `$0200-$03FF`: Input buffer & system state
- `$0400-$07FF`: Text page 1
- `$0800-$0BFF`: Text page 2
- `$2000-$3FFF`: Hi-res page 1
- `$4000-$5FFF`: Hi-res page 2
- `$C000-$CFFF`: I/O space (soft switches, slots)
- `$D000-$FFFF`: ROM & language card area

### Important Constants (from VA2MBus)
```csharp
const ushort KBD_ = 0xC000;           // Keyboard data
const ushort KEYSTRB_ = 0xC010;       // Keyboard strobe
const ushort BUTTON0_ = 0xC061;       // Push button 0
const ushort RD_TEXT_ = 0xC01A;       // Read text mode status
const ushort SETTXT_ = 0xC051;        // Set text mode
const ushort SETHIRES_ = 0xC057;      // Set hi-res mode
```

## Dependency Injection

### Service Registration (Program.cs)
Services are registered in `Pandowdy/Program.cs`:
```csharp
// Core services
services.AddSingleton<MemoryPool>();
services.AddSingleton<IDirectMemoryPoolReader>(sp => sp.GetRequiredService<MemoryPool>());
services.AddSingleton<ICpu, CPUAdapter>();
services.AddSingleton<IAppleIIBus, VA2MBus>();

// Services from Pandowdy.EmuCore.Services
services.AddSingleton<IFrameProvider, FrameProvider>();
services.AddSingleton<IEmulatorState, EmulatorStateProvider>();
services.AddSingleton<ICharacterRomProvider, CharacterRomProvider>();
services.AddSingleton<IDisplayBitmapRenderer, LegacyBitmapRenderer>();
services.AddSingleton<IFrameGenerator, FrameGenerator>();

// SystemStatusProvider implements multiple interfaces
services.AddSingleton<SystemStatusProvider>();
services.AddSingleton<ISystemStatusProvider>(sp => sp.GetRequiredService<SystemStatusProvider>());
services.AddSingleton<ISoftSwitchResponder>(sp => sp.GetRequiredService<SystemStatusProvider>());

// Main emulator
services.AddSingleton<VA2M>();
```

## Performance Considerations

### Emulation Speed
- Target: 1.023 MHz (Apple II clock speed)
- VBlank events: 60Hz (~17,063 cycles per frame)
- Throttling: Configurable in VA2M class

### Optimization Notes
- Double buffering for frame rendering (FrameProvider)
- Memory-mapped I/O uses dictionary lookup (VA2MBus)
- Soft switch state cached in SystemStatusProvider
- Reactive streams for UI updates (Rx.NET)

## Common Patterns

### State Management
```csharp
// Immutable snapshots with records
public record StateSnapshot(ushort PC, byte SP, ulong Cycles, ...);

// Observable streams for UI
public IObservable<StateSnapshot> Stream { get; }

// Mutation via builder pattern
provider.Mutate(builder => builder.StateTextMode = true);
```

### Event Handling
```csharp
// VBlank event for frame synchronization
vb.VBlank += OnVBlank;

// Frame available event for rendering
frameProvider.FrameAvailable += OnFrameAvailable;
```

### Error Handling
```csharp
// Null validation
ArgumentNullException.ThrowIfNull(parameter);

// Size validation
ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);

// State validation with descriptive messages
if (condition)
    throw new InvalidOperationException("Detailed message explaining the issue");
```

## When Adding New Features

### Checklist
1. ? Determine if it's a Core Domain class or Service
2. ? Place in appropriate directory (root vs Services/)
3. ? Create interface in Pandowdy.EmuCore/Interfaces/
4. ? Register in DI container (Program.cs) if needed
5. ? Create mirrored test file with comprehensive coverage
6. ? Ensure all 553+ tests still pass
7. ? Use `git mv` for any file moves
8. ? Update this file if adding new architectural patterns

## Resources

- **Main Branch**: `more_di_work`
- **GitHub**: https://github.com/markdavidlong/Pandowdy
- **Legacy Libraries**: 
  - 6502.NET (CPU emulator)
  - CiderPress2 (disk image handling)
  - AppleSAWS (disassembler)

## Questions?

If something isn't clear or documented here, check:
1. Existing similar code in the project
2. Test files for usage examples
3. VA2MBus.cs for I/O handling patterns
4. Program.cs for DI registration patterns

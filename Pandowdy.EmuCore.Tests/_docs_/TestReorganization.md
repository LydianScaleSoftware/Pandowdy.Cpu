# Test Reorganization Summary

## Overview

Cleaned up and reorganized the test structure for Pandowdy emulator components, moving tests to appropriate files and creating structured placeholders for future test development.

---

## Changes Made

### 1. **Deleted**
- ? `Pandowdy.Tests/TestVA2M.cs` - Old test file with misplaced tests

### 2. **Created**
- ? `Pandowdy.Tests/LegacyBitmapRendererTests.cs` - Tests for bitmap rendering
- ? `Pandowdy.Tests/VA2MTests.cs` - Placeholder for VA2M-specific tests

---

## File: `LegacyBitmapRendererTests.cs`

**Purpose**: Tests for the `LegacyBitmapRenderer` class, which handles Apple II video rendering.

### Tests Implemented (11 tests)

#### `GetAddressForXY` Tests
Tests the critical memory address calculation for Apple II video display:

1. ? **TextPage1_BasicPositions** - Verify text page 1 address calculations
   - Row 0, Col 0 ? `$0400`
   - Row 1, Col 0 ? `$0480` (128-byte offset)
   - Row 8, Col 0 ? `$0428` (second group)
   - Row 16, Col 0 ? `$0450` (third group)

2. ? **TextPage2_BaseAddress** - Verify text page 2 uses `$0800` base

3. ? **HiResPage1_BasicPositions** - Verify hi-res page 1 addresses
   - Base address `$2000`
   - CellRowOffset support
   - Complex interleaving

4. ? **HiResPage2_BasicPositions** - Verify hi-res page 2 addresses
   - Base address `$4000`
   - All position calculations

5. ? **MixedMode_BottomTextArea** - Verify mixed mode text/graphics split
   - Rows >= 20 use text page addresses

6. ? **InvalidCoordinates_ReturnsNegativeOne** - Boundary validation
   - X >= 40 ? `-1`
   - Y >= 24 ? `-1`
   - Negative coordinates ? `-1`

7. ? **TextPage1_BoundaryTests** (Theory test with 4 cases)
   - Corner positions: (0,0), (39,0), (0,23), (39,23)

8. ? **LoResMode_UsesTextPageLayout** - Lo-res shares text memory layout

### Apple II Memory Layout Reference

#### Text/LoRes Pages
```
Page 1: $0400-$07FF
Page 2: $0800-$0BFF

Row layout (interleaved in groups of 8):
Rows 0-7:   +$000, +$080, +$100, +$180, +$200, +$280, +$300, +$380
Rows 8-15:  +$028, +$0A8, +$128, +$1A8, +$228, +$2A8, +$328, +$3A8
Rows 16-23: +$050, +$0D0, +$150, +$1D0, +$250, +$2D0, +$350, +$3D0
```

#### Hi-Res Pages
```
Page 1: $2000-$3FFF
Page 2: $4000-$5FFF

Formula: Base + (row % 8) * 128 + (row / 8) * 40 + (cellRowOffset * 0x400) + col
```

### Future Tests (Placeholders)
- `RenderScreen` - Full screen rendering
- `RenderTextOrGRCell` - Text/graphics cell rendering
- `RenderHiresCell` - Hi-res cell rendering
- `RenderTextCell` - Text character rendering
- `RenderGrCell` - Lo-res graphics cell
- `InsertHgrByteAt` - Hi-res byte placement
- `MakeGrColor` - Lo-res color generation

**Requirements for future tests**:
- Mock `ICharacterRomProvider`
- Mock `RenderContext` with test memory
- Bitmap output verification

---

## File: `VA2MTests.cs`

**Purpose**: Placeholder for VA2M (main emulator) tests.

### Current State
- ? 1 placeholder test (ensures file compiles)
- Comprehensive TODO comments for future tests

### Planned Test Categories

#### 1. Constructor Tests
- Dependency injection validation
- ROM loading on initialization

#### 2. Property Tests
- `ThrottleEnabled`
- `TargetHz`
- `SystemClock`

#### 3. ROM Loading Tests
- `TryLoadEmbeddedRom` success/failure
- ROM validation
- Checksum verification

#### 4. Throttling Tests
- `ThrottleOneCycle` timing accuracy
- TargetHz adherence
- Fast mode (throttle disabled)

#### 5. Clock and Reset Tests
- `Clock()` increments system clock
- `Reset()` clears state
- `UserReset()` behavior

#### 6. Key Injection Tests
- `InjectKey()` sets memory correctly
- High bit enforcement
- ASCII range handling

#### 7. Push Button Tests
- `SetPushButton()` state changes
- All 3 buttons (0, 1, 2)

#### 8. VBlank Tests
- Flash toggle (~2.1 Hz)
- Render invocation
- Frame timing

### Integration Test Ideas
Documented but not implemented (suggest separate project):
1. Full boot sequence
2. Video mode switching
3. Keyboard input flow
4. Performance benchmarks
5. State snapshots/restore

**Challenge**: VA2M has complex dependencies:
- `IEmulatorState`
- `IFrameProvider`
- `ISystemStatusProvider`
- `IAppleIIBus`
- `MemoryPool`
- `IVideoSubsystem`

**Recommendation**: Create test helper builders/factories for dependencies.

---

## Test Statistics

| File | Tests | Status |
|------|-------|--------|
| `SystemStatusProviderTests.cs` | 59 | ? All passing |
| `LegacyBitmapRendererTests.cs` | 11 | ? All passing |
| `VA2MTests.cs` | 1 | ? Placeholder passing |
| `SoftSwitchResponderTests.cs` | 7 | ? All passing (existing) |
| `TestMemoryPool.cs` | 27 | ? All passing (existing) |
| **Total** | **105** | **? 100% passing** |

---

## Project Structure

```
Pandowdy.Tests/
??? Services/
?   ??? SystemStatusProviderTests.cs       (59 tests)
?   ??? SystemStatusProviderTests.md       (documentation)
??? LegacyBitmapRendererTests.cs          (11 tests)
??? VA2MTests.cs                           (1 placeholder)
??? SoftSwitchResponderTests.cs           (7 tests - existing)
??? TestMemoryPool.cs                      (27 tests - existing)
```

---

## Benefits

### 1. **Better Organization**
- Tests now live with the classes they test
- Clear separation between unit and integration tests
- Future test placeholders documented

### 2. **Apple II Accuracy**
- Comprehensive testing of memory address calculations
- Validates interleaved memory layout
- Tests mixed mode behavior

### 3. **Maintainability**
- TODOs clearly documented
- Required dependencies identified
- Test patterns established

### 4. **Foundation for Future Work**
When rendering refactor happens:
- Tests can be expanded to cover new renderers
- Existing tests verify backward compatibility
- Clear structure for new test categories

---

## Next Steps

### High Priority
1. **Create test helpers** for VA2M dependencies
   - `TestEmulatorStateBuilder`
   - `MockFrameProvider`
   - `TestMemoryPoolBuilder`

2. **Expand `SoftSwitches` tests**
   - Already has 7 tests
   - Add comprehensive coverage for all switches

3. **Add `FrameProvider` tests**
   - Buffer swapping
   - Event emission
   - Thread safety

### Medium Priority
4. **Implement VA2M unit tests**
   - Start with simple property tests
   - Add throttling tests
   - Test key injection

5. **Expand `LegacyBitmapRenderer` tests**
   - Mock character ROM
   - Test rendering methods
   - Verify bitmap output

### Lower Priority (Integration)
6. **Create separate integration test project**
   - Full boot sequence
   - Video mode switches
   - Keyboard input end-to-end

---

## Running Tests

### Run All Tests
```bash
cd Pandowdy.Tests
dotnet test
```

### Run Specific Test File
```bash
# LegacyBitmapRenderer tests
dotnet test --filter "FullyQualifiedName~LegacyBitmapRendererTests"

# SystemStatusProvider tests
dotnet test --filter "FullyQualifiedName~SystemStatusProviderTests"

# VA2M tests
dotnet test --filter "FullyQualifiedName~VA2MTests"
```

### Run Specific Test
```bash
dotnet test --filter "FullyQualifiedName~GetAddressForXY_TextPage1_BasicPositions"
```

---

## Notes

### Why Move GetAddressForXY?
- Previously tested via reflection in `TestVA2M.cs`
- Method now lives in `LegacyBitmapRenderer.cs`
- Tests should live with the code they test
- Makes refactoring easier

### Why Placeholder VA2M Tests?
- VA2M is complex with many dependencies
- Mocking infrastructure needed first
- Placeholder documents what needs testing
- Prevents "TODO" comments scattered in code

### Apple II Memory Trivia
The interleaved memory layout in Apple II was designed for the video hardware scanner. Each row offset allows the CRT beam to have time to move to the next line without complex timing logic. This "feature" made programming harder but hardware simpler!

---

*Reorganization completed: 2025-01-XX*  
*Last updated: 2025-01-XX*

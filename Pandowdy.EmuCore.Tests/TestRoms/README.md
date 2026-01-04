# Test ROM Files for SystemRomProviderTests

This directory contains test ROM files used by the SystemRomProvider unit tests.

## Files

- **valid16kb.rom** (16,384 bytes / 0x4000) - Valid Apple IIe ROM size with test pattern
- **valid16kb_ff.rom** (16,384 bytes / 0x4000) - Valid size, all 0xFF (for reload tests)
- **toosmall.rom** (8,192 bytes / 0x2000) - Too small (should fail validation)
- **toolarge.rom** (20,480 bytes / 0x5000) - Too large (should fail validation)

## Purpose

These files are used to test ROM loading validation:
- Correct size acceptance
- Size validation (too small/too large rejection)
- File reading functionality
- Error message generation
- Pattern verification for offset error detection
- ROM reload functionality

## valid16kb.rom Pattern

The valid16kb.rom file uses a **0x00-0xFE** repeating pattern (255 values):

```
Offset    Value   Description
0x0000    0x00    First byte
0x0001    0x01    Second byte
...
0x00FE    0xFE    Byte 254
0x00FF    0x00    Byte 255 (wraps to 0)
0x0100    0x01    Page 1 starts with 0x01
0x0200    0x02    Page 2 starts with 0x02
...
0x1000    0x10    Page 16 (Monitor ROM area)
0x2000    0x20    Page 32 (BASIC ROM area)
...
0x3FFF    0x3F    Last byte (0x3FFF % 255 = 63 = 0x3F)
```

### Why This Pattern?

This pattern makes each **256-byte page start with a unique value**, making it easy to detect:

- ✅ **Off-by-page errors** - Reading wrong page shows up immediately
- ✅ **Off-by-256 errors** - Incorrect base address is obvious
- ✅ **Bank switching errors** - Wrong bank selection is detectable
- ✅ **Address calculation errors** - Math mistakes are caught

For example:
- If code should read offset `0x1000` but accidentally reads `0x0000`, it gets `0x00` instead of `0x10`
- If code should read offset `0x2000` but accidentally reads `0x2100`, it gets `0x21` instead of `0x20`

This is much better than a pattern that repeats every 256 bytes (like `i & 0xFF`), which would hide page-boundary errors.

## Generation

The test files are generated and managed by PowerShell scripts:

```powershell
# valid16kb.rom - Pattern for error detection
$size = 0x4000
$rom = New-Object byte[] $size
for ($i = 0; $i -lt $size; $i++) { 
    $rom[$i] = $i % 255  # Pattern: 0x00-0xFE repeating
}
[System.IO.File]::WriteAllBytes("valid16kb.rom", $rom)

# valid16kb_ff.rom - All 0xFF
$rom = New-Object byte[] $size
for ($i = 0; $i -lt $size; $i++) { $rom[$i] = 0xFF }
[System.IO.File]::WriteAllBytes("valid16kb_ff.rom", $rom)

# toosmall.rom - 8KB (wrong size)
$rom = New-Object byte[] 0x2000
[System.IO.File]::WriteAllBytes("toosmall.rom", $rom)

# toolarge.rom - 20KB (wrong size)
$rom = New-Object byte[] 0x5000
for ($i = 0; $i -lt 0x5000; $i++) { $rom[$i] = 0xFF }
[System.IO.File]::WriteAllBytes("toolarge.rom", $rom)
```

## Test Usage

The SystemRomProviderTests use these files to verify:

1. ✅ **Valid ROM loading** - `valid16kb.rom` and `valid16kb_ff.rom` load successfully
2. ✅ **Size validation (too small)** - `toosmall.rom` is rejected (8KB instead of 16KB)
3. ✅ **Size validation (too large)** - `toolarge.rom` is rejected (20KB instead of 16KB)
4. ✅ **Pattern verification** - Each page in `valid16kb.rom` starts with expected unique value
5. ✅ **Read consistency** - Multiple reads return same value
6. ✅ **Write protection** - ROM remains unchanged after write attempts
7. ✅ **ROM reload** - Can load different ROM file into existing provider

## File Sizes

| File | Size (bytes) | Size (hex) | Purpose |
|------|--------------|------------|---------|
| **valid16kb.rom** | 16,384 | 0x4000 | Valid test case with pattern |
| **valid16kb_ff.rom** | 16,384 | 0x4000 | Valid test case (all 0xFF) |
| **toosmall.rom** | 8,192 | 0x2000 | Invalid (too small) |
| **toolarge.rom** | 20,480 | 0x5000 | Invalid (too large) |

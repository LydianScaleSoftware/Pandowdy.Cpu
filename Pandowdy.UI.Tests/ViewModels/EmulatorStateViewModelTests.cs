// Copyright 2026 Mark D. Long
// Licensed under the Apache License, Version 2.0
// See LICENSE file for details

namespace Pandowdy.UI.Tests.ViewModels;

/// <summary>
/// Tests for EmulatorStateViewModel - binds emulator state to UI.
/// </summary>
/// <remarks>
/// Tests skipped pending refactor to pull-based architecture.
/// EmulatorStateViewModel now polls IEmulatorCoreInterface directly instead of subscribing to IEmulatorState.
/// LineNumber property removed (will be reimplemented later).
/// </remarks>
public class EmulatorStateViewModelTests
{
    // TODO: Rewrite tests for pull-based architecture
    // - Mock IEmulatorCoreInterface with CpuState and TotalCycles
    // - Mock IRefreshTicker for polling timing
    // - Remove LineNumber tests (property removed)
    // - Test polling behavior instead of reactive push
}

// Copyright 2026 Mark D. Long
// Licensed under the Apache License, Version 2.0
// See LICENSE file for details

using CommonUtil;
using System.Diagnostics;

namespace Pandowdy.EmuCore.DiskII.Importers;

/// <summary>
/// Minimal no-op implementation of MessageLog for DiskArc library usage.
/// Silently discards all log messages.
/// </summary>
internal sealed class NullMessageLog : MessageLog
{
    public override void Clear()
    {
        // No-op
    }

    public override void Log(Priority prio, string message)
    {
        // No-op - discard all messages
        Debug.WriteLine($"DiskArc: [{prio}] {message}");
    }
}

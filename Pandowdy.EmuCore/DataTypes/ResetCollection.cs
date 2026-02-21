// // Copyright 2026 Mark D. Long
// // Licensed under the Apache License, Version 2.0
// // See LICENSE file for details
//
//

using System.Diagnostics;
using Pandowdy.EmuCore.Interfaces;

namespace Pandowdy.EmuCore.DataTypes;

public class ResetCollection(IEnumerable<IResetable> resetables)
{
    private readonly IEnumerable<IResetable> _resetables = resetables;

    public void ResetAll()
    {
        Debug.WriteLine($"Calling ResetAll() on ResetCollection ({_resetables.Count()} item(s))");

        foreach (var r in _resetables)
        {
            Debug.WriteLine($" ... resetting {r}");
            r.Reset();
        }
    }
}

// Copyright 2026 Mark D. Long
// Licensed under the Apache License, Version 2.0
// See LICENSE file for details


namespace Pandowdy.EmuCore.Interfaces;
public interface IPandowdyMemory
{
    int Size { get; }
    byte Read(ushort address);
    void Write(ushort address, byte data);

    byte this[ushort address] { get; set; }
}


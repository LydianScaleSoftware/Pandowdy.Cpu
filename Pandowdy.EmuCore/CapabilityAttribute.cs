// // Copyright 2026 Mark D. Long
// // Licensed under the Apache License, Version 2.0
// // See LICENSE file for details
//
//

namespace Pandowdy.EmuCore;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class CapabilityAttribute(Type interfaceType) : Attribute
{
    public Type InterfaceType { get; } = interfaceType;
}

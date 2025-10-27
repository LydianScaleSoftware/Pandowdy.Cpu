using System;
using Emulator;

namespace Pandowdy.Core;

/// <summary>
/// VA2M Memory implementation: fulfills Emulator.IMemory for the CPU/bus,
/// and exposes Pandowdy.Core.IMappedMemory for UI updates.
/// </summary>
public sealed class VA2MMemory : IMemory, IMappedMemory
{
 private readonly byte[] _data;

 public VA2MMemory(int size)
 {
 _data = new byte[size];
 }

 // Emulator.IMemory
 public int Size => _data.Length;
 public byte[] DataArray() => _data;
 public byte this[ushort address]
 {
 get => _data[address];
 set
 {
 _data[address] = value;
 // Raise mapped-memory event
 MemoryWritten?.Invoke(this, new MemoryAccessEventArgs { Address = address, Value = value, Length =1 });
 }
 }
 public byte Read(ushort address) => _data[address];
 public void Write(ushort address, byte data)
 {
 _data[address] = data;
 MemoryWritten?.Invoke(this, new MemoryAccessEventArgs { Address = address, Value = data, Length =1 });
 }
 public void WriteBlock(ushort offset, params byte[] data)
 {
 data.CopyTo(_data, offset);
 MemoryBlockWritten?.Invoke(this, new MemoryAccessEventArgs { Address = offset, Value = null, Length = data.Length });
 }
 public byte[] ReadBlock(ushort address, int length)
 {
 int availableLength = Math.Max(0, _data.Length - address);
 int readLength = Math.Min(length, availableLength);
 var buffer = new byte[readLength];
 Array.Copy(_data, address, buffer,0, readLength);
 return buffer;
 }

 // Pandowdy.Core.IMappedMemory
 public event EventHandler<MemoryAccessEventArgs>? MemoryWritten;
 public event EventHandler<MemoryAccessEventArgs>? MemoryBlockWritten;

 // IMappedMemory requires Read for UI after block writes
 byte IMappedMemory.Read(ushort address) => Read(address);
}

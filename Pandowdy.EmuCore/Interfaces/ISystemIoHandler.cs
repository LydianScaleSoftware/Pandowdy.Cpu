using Emulator;

namespace Pandowdy.EmuCore.Interfaces;



public interface ISystemIoHandler : IMemory
{
    //public void EnqueueKey(byte key);           // For keyboard input
    public void UpdateVBlankCounter(long counter);      // For VBlank timing

    public void Reset(); // Resets any subsystems
}

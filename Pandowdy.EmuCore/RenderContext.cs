using Pandowdy.EmuCore.Interfaces;

namespace Pandowdy.EmuCore
{
    public struct RenderContext(
        BitmapDataArray frameBuffer,
        IDirectMemoryPoolReader memory,
        ISystemStatusProvider status)
    {
        public BitmapDataArray FrameBuffer = frameBuffer ?? throw new ArgumentNullException(nameof(frameBuffer));
        public IDirectMemoryPoolReader Memory = memory ?? throw new ArgumentNullException(nameof(memory));
        public ISystemStatusProvider SystemStatus = status ?? throw new ArgumentNullException(nameof(status));

        public readonly bool IsTextMode => SystemStatus.StateTextMode;
        public readonly bool IsMixed => SystemStatus.StateMixed;
        public readonly bool IsHiRes => SystemStatus.StateHiRes;
        public readonly bool IsPage2 => SystemStatus.StatePage2;

        public readonly void ClearBuffer() { FrameBuffer.Clear(); }
    }
}

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

    /// <summary>
    /// Generates Apple II video frames by coordinating bitmap rendering,
    /// memory access, and system status. Produces annotated frames for
    /// downstream consumers (e.g., NTSC post-processing, GUI display).
    /// </summary>
    public class FrameGenerator : IFrameGenerator
    {
        private IFrameProvider _frameProvider;
        private IDirectMemoryPoolReader _memReader;
        private ISystemStatusProvider _statusProvider;
        private IDisplayBitmapRenderer _renderer;

        public FrameGenerator(IFrameProvider frameProvider, IDirectMemoryPoolReader memReader, ISystemStatusProvider statusProvider, IDisplayBitmapRenderer renderer)
        {
            ArgumentNullException.ThrowIfNull(frameProvider);
            ArgumentNullException.ThrowIfNull(memReader);
            ArgumentNullException.ThrowIfNull(statusProvider);
            ArgumentNullException.ThrowIfNull(renderer);

            _frameProvider = frameProvider;
            _memReader = memReader;
            _statusProvider = statusProvider;
            _renderer = renderer;
        }

        public RenderContext AllocateRenderContext()
        {
            var context = new RenderContext(
                _frameProvider.BorrowWritable(),
                _memReader,
                _statusProvider);

            return context;
        }
        
        public void RenderFrame(RenderContext context)
        {
            context.ClearBuffer();

            //    Call Renderer
            _renderer.Render(context);

            // Annotate frame with display mode metadata for downstream consumers
            // (e.g., NTSC renderer) so they don't need ISystemStatusProvider reference
            _frameProvider.IsGraphics = !_statusProvider.StateTextMode;
            _frameProvider.IsMixed = _statusProvider.StateMixed;

            _frameProvider.CommitWritable();

        }


    }
}

using Emulator;

namespace Pandowdy.EmuCore
{
    public static class Utility
    {


        /// <summary>
        /// Validates that an IMemory instance is a given size.
        /// </summary>
        /// <param name="memory">The memory instance to validate.</param>
        /// <param name="paramName">The parameter name for exception messages.</param>
        /// <param name="expectedSize">The expected size of the memory in bytes.</param>
        /// <returns>The validated memory instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown if memory is null.</exception>
        /// <exception cref="ArgumentException">Thrown if memory size is not exactly 16KB.</exception>
        public static IMemory ValidateIMemorySize(IMemory memory, string paramName, UInt16 expectedSize)
        {
            ArgumentNullException.ThrowIfNull(memory, paramName);

            if (memory.Size != expectedSize)
            {
                throw new ArgumentException(
                    $"Memory size must be exactly {expectedSize} bytes (0x{expectedSize:X}). " +
                    $"Actual size: {memory.Size} (0x{memory.Size:X})",
                    paramName);
            }

            return memory;
        }

    }
}

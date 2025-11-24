using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pandowdy.UI
{
    public static class MemoryPool
    {
        public static byte[] Pool = new byte[0x27F00]; // 163584 bytes

        public static Memory<byte>? R_0000_01FF = M1;
        public static Memory<byte>? R_0200_03FF = M2;
        public static Memory<byte>? R_0400_07FF = M3;
        public static Memory<byte>? R_0800_1FFF = M4;
        public static Memory<byte>? R_2000_3FFF = M5;
        public static Memory<byte>? R_4000_5FFF = M6;
        public static Memory<byte>? R_6000_BFFF = M7;
        public static Memory<byte>? R_C000_C0FF = IO;
        public static Memory<byte>? R_C100_C1FF = INT1ROM;
        public static Memory<byte>? R_C200_C2FF = INT2ROM;
        public static Memory<byte>? R_C300_C3FF = INT3ROM;
        public static Memory<byte>? R_C400_C4FF = INT4ROM;
        public static Memory<byte>? R_C500_C5FF = INT5ROM;
        public static Memory<byte>? R_C600_C6FF = INT6ROM;
        public static Memory<byte>? R_C700_C7FF = INT7ROM;
        public static Memory<byte>? R_C800_CFFF = INTEXTROM;
        public static Memory<byte>? R_D000_DFFF = ROM1;
        public static Memory<byte>? R_E000_FFFF = ROM2;

        public static Memory<byte>? W_0000_01FF = M1;
        public static Memory<byte>? W_0200_03FF = M2;
        public static Memory<byte>? W_0400_07FF = M3;
        public static Memory<byte>? W_0800_1FFF = M4;
        public static Memory<byte>? W_2000_3FFF = M5;
        public static Memory<byte>? W_4000_5FFF = M6;
        public static Memory<byte>? W_6000_BFFF = M7;
        public static Memory<byte>? W_C000_C0FF = IO;

        public static Memory<byte>? W_C100_C1FF = null; // These should remain Read Only, but certain cards could have
        public static Memory<byte>? W_C200_C2FF = null; //  swapped in writeable memory instead of ROM.
        public static Memory<byte>? W_C300_C3FF = null; // We'll let later card logic figure that out.  For now this will do.
        public static Memory<byte>? W_C400_C4FF = null;
        public static Memory<byte>? W_C500_C5FF = null;
        public static Memory<byte>? W_C600_C6FF = null;
        public static Memory<byte>? W_C700_C7FF = null;

        public static Memory<byte>? W_C800_CFFF = null; 
        public static Memory<byte>? W_E000_FFFF = null;

        // Main Memory regions
        public static Memory<byte> M1 = new (Pool, 0x0000, 0x0200);
        public static Memory<byte> M2 = new(Pool, 0x0200, 0x0200);
        public static Memory<byte> M3 = new(Pool, 0x0400, 0x0400);
        public static Memory<byte> M4 = new(Pool, 0x0800, 0x1800);
        public static Memory<byte> M5 = new(Pool, 0x2000, 0x2000);
        public static Memory<byte> M6 = new(Pool, 0x4000, 0x2000);
        public static Memory<byte> M7 = new(Pool, 0x6000, 0x6000);
        public static Memory<byte> M8a = new(Pool, 0xc000, 0x1000);
        public static Memory<byte> M8b = new(Pool, 0xd000, 0x1000);
        public static Memory<byte> M9 = new(Pool, 0xe000, 0x2000);

        // Auxiliary Memory regions
        public static Memory<byte> A1 = new(Pool, 0x10000, 0x0200);
        public static Memory<byte> A2 = new(Pool, 0x10200, 0x0200);
        public static Memory<byte> A3 = new(Pool, 0x10400, 0x0400);
        public static Memory<byte> A4 = new(Pool, 0x10800, 0x1800);
        public static Memory<byte> A5 = new(Pool, 0x12000, 0x2000);
        public static Memory<byte> A6 = new(Pool, 0x14000, 0x2000);
        public static Memory<byte> A7 = new(Pool, 0x16000, 0x6000);
        public static Memory<byte> A8a = new(Pool, 0x1c000, 0x1000);
        public static Memory<byte> A8b = new(Pool, 0x1d000, 0x1000);
        public static Memory<byte> A9 = new(Pool, 0x1e000, 0x2000);

        // IO Scrap Storage (Non-reliable/Volatile)
        public static Memory<byte> IO = new(Pool, 0x20000, 0x100);

        // Internal Slot ROMs
        public static Memory<byte> INT1ROM = new(Pool, 0x20100, 0x100);
        public static Memory<byte> INT2ROM = new(Pool, 0x20200, 0x100);
        public static Memory<byte> INT3ROM = new(Pool, 0x20300, 0x100);
        public static Memory<byte> INT4ROM = new(Pool, 0x20400, 0x100);
        public static Memory<byte> INT5ROM = new(Pool, 0x20500, 0x100);
        public static Memory<byte> INT6ROM = new(Pool, 0x20600, 0x100);
        public static Memory<byte> INT7ROM = new(Pool, 0x20700, 0x100);
        public static Memory<byte> INTEXTROM = new(Pool, 0x20800, 0x800);

        // System ROM
        public static Memory<byte> ROM1 = new(Pool, 0x21000, 0x1000);
        public static Memory<byte> ROM2 = new(Pool, 0x22000, 0x2000);

        // Slot ROMs
        public static Memory<byte> S1ROM = new(Pool, 0x24000, 0x100);
        public static Memory<byte> S2ROM = new(Pool, 0x24100, 0x100);
        public static Memory<byte> S3ROM = new(Pool, 0x24200, 0x100);
        public static Memory<byte> S4ROM = new(Pool, 0x24300, 0x100);
        public static Memory<byte> S5ROM = new(Pool, 0x24400, 0x100);
        public static Memory<byte> S6ROM = new(Pool, 0x24500, 0x100);
        public static Memory<byte> S7ROM = new(Pool, 0x24600, 0x100);

        // Slot Extended ROMs
        public static Memory<byte> S1EXTROM = new(Pool, 0x24700, 0x800); 
        public static Memory<byte> S2EXTROM = new(Pool, 0x24f00, 0x800);
        public static Memory<byte> S3EXTROM = new(Pool, 0x25700, 0x800);
        public static Memory<byte> S4EXTROM = new(Pool, 0x25f00, 0x800);
        public static Memory<byte> S5EXTROM = new(Pool, 0x26700, 0x800);
        public static Memory<byte> S6EXTROM = new(Pool, 0x26f00, 0x800);
        public static Memory<byte> S7EXTROM = new(Pool, 0x27700, 0x800);

        // Ends at 0x27f00 (163584 bytes)

        static MemoryPool()
        {
            // Initialize Pool with random bytes
            var rand = new Random();
            rand.NextBytes(Pool);
        }
    }
}

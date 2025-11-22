using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pandowdy.Core
{
    internal class BitmapDataArray 
    {
        private const int lines = 280;
        private const int strideCols = 81; // 81 bytes per line to cover 561 pixels (561 / 8 = 70.125, round up to 71, use 81 for alignment)
        private const int stridePixels = strideCols*8;  
        private byte[] data = new byte[lines * strideCols];

        public void Clear()
        {
            Array.Clear(data, 0, data.Length);
        }

        public void SetPixel(int x, int y)
        {
            if (x < 0 || x >= stridePixels)
            {
                throw new ArgumentOutOfRangeException(nameof(x), $"x must be between 0 and {stridePixels} inclusive.");
            }
            if (y < 0 || y >= 280)
            {
                throw new ArgumentOutOfRangeException(nameof(y), "y must be between 0 and 279 inclusive.");
            }
            int index = y * stridePixels + (x / 8);
            byte mask = (byte)(0x80 >> (x % 8));
            data[index] |= mask;
        }

        public void SetDoublePixel(int x, int y) // Used in 40-col mode to set two adjacent pixels (even and odd)
        {
            if (x < 0 || x >= stridePixels/2)
            {
                throw new ArgumentOutOfRangeException(nameof(x), $"x must be between 0 and {stridePixels/2} inclusive.");
            }
            if (y < 0 || y >= 280)
            {
                throw new ArgumentOutOfRangeException(nameof(y), "y must be between 0 and 279 inclusive.");
            }
            SetPixel(x/2, y);
            SetPixel(x/2 + 1, y);
        }

        public void ClearDoublePixel(int x, int y) // Used in 40-col mode to clear two adjacent pixels (even and odd)
        {
            if (x < 0 || x >= stridePixels / 2)
            {
                throw new ArgumentOutOfRangeException(nameof(x), $"x must be between 0 and {stridePixels / 2} inclusive.");
            }
            if (y < 0 || y >= 280)
            {
                throw new ArgumentOutOfRangeException(nameof(y), "y must be between 0 and 279 inclusive.");
            }
            ClearPixel(x / 2, y);
            ClearPixel(x / 2 + 1, y);
        }
        public void ClearPixel(int x, int y)
        {
            if (x < 0 || x >= stridePixels)
            {
                throw new ArgumentOutOfRangeException(nameof(x), $"x must be between 0 and {stridePixels} inclusive.");
            }
            if (y < 0 || y >= 280)
            {
                throw new ArgumentOutOfRangeException(nameof(y), "y must be between 0 and 279 inclusive.");
            }
            int index = y * stridePixels + (x / 8);
            byte mask = (byte)(0x80 >> (x % 8));
            data[index] &= (byte)~mask;
        }

        public bool GetPixel(int x, int y)
        {
            if (x < 0 || x >= stridePixels)
            {
                throw new ArgumentOutOfRangeException(nameof(x), $"x must be between 0 and {stridePixels} inclusive.");
            }
            if (y < 0 || y >= 280)
            {
                throw new ArgumentOutOfRangeException(nameof(y), "y must be between 0 and 279 inclusive.");
            }
            int index = y * stridePixels + (x / 8);
            byte mask = (byte)(0x80 >> (x % 8));
            return (data[index] & mask) != 0;
        }

        public Span<bool> GetPixelSpan(int x, int y, int length)
        {
            if (x < 0 || x + length > stridePixels)
            {
                throw new ArgumentOutOfRangeException(nameof(x), $"x must be between 0 and { stridePixels } inclusive.");
            }
            if (y < 0 || y >= 280)
            {
                throw new ArgumentOutOfRangeException(nameof(y), "y must be between 0 and 279 inclusive.");
            }
            Span<bool> span = new bool[length];
            for (int i = 0; i < length; i++)
            {
                span[i] = GetPixel(x + i, y);
            }
            return span;
        }

        public ReadOnlySpan<byte> GetRowDataSpan(int row)
        {
            if (row < 0 || row >= lines)
            {
                throw new ArgumentOutOfRangeException(nameof(row), "row must be between 0 and 279 inclusive.");
            }
            return new ReadOnlySpan<byte>(data, row * stridePixels, strideCols);
        }

    }
}

using SkiaSharp;
using System;

namespace Pixelium.Core.Processors
{
    public enum FlipDirection
    {
        Horizontal,
        Vertical
    }

    public class FlipProcessor : IImageProcessor
    {
        private readonly FlipDirection _direction;

        public FlipProcessor(FlipDirection direction)
        {
            _direction = direction;
        }

        public unsafe bool Process(SKBitmap bitmap)
        {
            if (bitmap == null || bitmap.ColorType != SKColorType.Bgra8888)
                return false;

            int width = bitmap.Width;
            int height = bitmap.Height;
            int bytesPerPixel = 4;

            var pixels = bitmap.GetPixels();
            var ptr = (byte*)pixels.ToPointer();

            if (_direction == FlipDirection.Horizontal)
            {
                FlipHorizontal(ptr, width, height, bytesPerPixel);
            }
            else
            {
                FlipVertical(ptr, width, height, bytesPerPixel);
            }

            return true;
        }

        private unsafe void FlipHorizontal(byte* ptr, int width, int height, int bytesPerPixel)
        {
            int rowSize = width * bytesPerPixel;
            byte[] tempRow = new byte[rowSize];

            for (int y = 0; y < height; y++)
            {
                int rowStart = y * rowSize;

                // Copy row to temp
                for (int x = 0; x < rowSize; x++)
                {
                    tempRow[x] = ptr[rowStart + x];
                }

                // Copy back reversed
                for (int x = 0; x < width; x++)
                {
                    int srcPixel = (width - 1 - x) * bytesPerPixel;
                    int dstPixel = x * bytesPerPixel;

                    ptr[rowStart + dstPixel] = tempRow[srcPixel];         // B
                    ptr[rowStart + dstPixel + 1] = tempRow[srcPixel + 1]; // G
                    ptr[rowStart + dstPixel + 2] = tempRow[srcPixel + 2]; // R
                    ptr[rowStart + dstPixel + 3] = tempRow[srcPixel + 3]; // A
                }
            }
        }

        private unsafe void FlipVertical(byte* ptr, int width, int height, int bytesPerPixel)
        {
            int rowSize = width * bytesPerPixel;
            byte[] tempRow = new byte[rowSize];

            for (int y = 0; y < height / 2; y++)
            {
                int topRow = y * rowSize;
                int bottomRow = (height - 1 - y) * rowSize;

                // Swap rows
                for (int x = 0; x < rowSize; x++)
                {
                    tempRow[x] = ptr[topRow + x];
                    ptr[topRow + x] = ptr[bottomRow + x];
                    ptr[bottomRow + x] = tempRow[x];
                }
            }
        }
    }
}

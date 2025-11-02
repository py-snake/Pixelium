using SkiaSharp;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

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

        public long ProcessingTimeMs { get; private set; }

        public FlipProcessor(FlipDirection direction)
        {
            _direction = direction;
        }

        public unsafe bool Process(SKBitmap bitmap)
        {
            if (bitmap == null || bitmap.ColorType != SKColorType.Bgra8888)
                return false;

            var stopwatch = Stopwatch.StartNew();

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

            stopwatch.Stop();
            ProcessingTimeMs = stopwatch.ElapsedMilliseconds;

            return true;
        }

        private unsafe void FlipHorizontal(byte* ptr, int width, int height, int bytesPerPixel)
        {
            int rowSize = width * bytesPerPixel;

            // Parallelize per row - each row is independent
            Parallel.For(0, height, y =>
            {
                byte[] tempRow = new byte[rowSize]; // Thread-local buffer
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
            });
        }

        private unsafe void FlipVertical(byte* ptr, int width, int height, int bytesPerPixel)
        {
            int rowSize = width * bytesPerPixel;

            // Parallelize row swapping - each pair swap is independent
            Parallel.For(0, height / 2, y =>
            {
                byte[] tempRow = new byte[rowSize]; // Thread-local buffer
                int topRow = y * rowSize;
                int bottomRow = (height - 1 - y) * rowSize;

                // Swap rows
                for (int x = 0; x < rowSize; x++)
                {
                    tempRow[x] = ptr[topRow + x];
                    ptr[topRow + x] = ptr[bottomRow + x];
                    ptr[bottomRow + x] = tempRow[x];
                }
            });
        }
    }
}

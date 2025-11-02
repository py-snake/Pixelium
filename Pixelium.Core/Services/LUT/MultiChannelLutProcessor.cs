using SkiaSharp;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Pixelium.Core.Processors;

namespace Pixelium.Core.Services.LUT
{
    public abstract class MultiChannelLutProcessor : IImageProcessor
    {
        protected readonly ILookupTableService _lutService;

        public long ProcessingTimeMs { get; private set; }

        protected MultiChannelLutProcessor(ILookupTableService lutService)
        {
            _lutService = lutService;
        }

        protected abstract byte[] GetRedLookupTable();
        protected abstract byte[] GetGreenLookupTable();
        protected abstract byte[] GetBlueLookupTable();

        public unsafe bool Process(SKBitmap bitmap)
        {
            if (bitmap == null || bitmap.ColorType != SKColorType.Bgra8888)
                return false;

            var stopwatch = Stopwatch.StartNew();

            var redLut = GetRedLookupTable();
            var greenLut = GetGreenLookupTable();
            var blueLut = GetBlueLookupTable();

            var pixels = bitmap.GetPixels();
            var ptr = (byte*)pixels.ToPointer();

            Parallel.For(0, bitmap.Height, y =>
            {
                byte* rowPtr = ptr + (y * bitmap.Width * 4);

                for (int x = 0; x < bitmap.Width * 4; x += 4)
                {
                    byte red = rowPtr[x + 2];
                    byte green = rowPtr[x + 1];
                    byte blue = rowPtr[x];

                    // Grayscale calculation: 0.299*R + 0.587*G + 0.114*B
                    // Use int to safely sum, then clamp to prevent overflow from rounding
                    int grayValue = redLut[red] + greenLut[green] + blueLut[blue];
                    byte gray = (byte)Math.Min(255, grayValue);

                    rowPtr[x] = gray;     // Blue
                    rowPtr[x + 1] = gray; // Green
                    rowPtr[x + 2] = gray; // Red
                    // Alpha remains unchanged
                }
            });

            stopwatch.Stop();
            ProcessingTimeMs = stopwatch.ElapsedMilliseconds;

            return true;
        }
    }
}

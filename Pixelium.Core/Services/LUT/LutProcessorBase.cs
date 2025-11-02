using SkiaSharp;
using System.Diagnostics;
using System.Threading.Tasks;
using Pixelium.Core.Processors;  // Add this using for IImageProcessor

namespace Pixelium.Core.Services.LUT
{
    public abstract class LutProcessorBase : IImageProcessor  // Explicitly implement the interface
    {
        protected readonly ILookupTableService _lutService;

        public long ProcessingTimeMs { get; private set; }

        protected LutProcessorBase(ILookupTableService lutService)
        {
            _lutService = lutService;
        }

        protected abstract byte[] GetLookupTable();

        // Explicitly implement the IImageProcessor interface
        public unsafe bool Process(SKBitmap bitmap)
        {
            if (bitmap == null || bitmap.ColorType != SKColorType.Bgra8888)
                return false;

            var stopwatch = Stopwatch.StartNew();

            var lut = GetLookupTable();
            if (lut == null) return false;

            var pixels = bitmap.GetPixels();
            var ptr = (byte*)pixels.ToPointer();

            Parallel.For(0, bitmap.Height, y =>
            {
                byte* rowPtr = ptr + (y * bitmap.Width * 4);

                for (int x = 0; x < bitmap.Width * 4; x += 4)
                {
                    rowPtr[x] = lut[rowPtr[x]];     // Blue
                    rowPtr[x + 1] = lut[rowPtr[x + 1]]; // Green
                    rowPtr[x + 2] = lut[rowPtr[x + 2]]; // Red
                    // Alpha remains unchanged
                }
            });

            stopwatch.Stop();
            ProcessingTimeMs = stopwatch.ElapsedMilliseconds;

            return true;
        }
    }
}

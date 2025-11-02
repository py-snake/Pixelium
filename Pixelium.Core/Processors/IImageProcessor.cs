using SkiaSharp;

namespace Pixelium.Core.Processors
{
    public interface IImageProcessor
    {
        bool Process(SKBitmap bitmap);

        /// <summary>
        /// Gets the processing time in milliseconds for the last Process() call.
        /// This measures only the actual filter algorithm execution time,
        /// excluding backup creation, layer cloning, and other overhead.
        /// </summary>
        long ProcessingTimeMs { get; }
    }
}

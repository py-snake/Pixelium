using SkiaSharp;
using System.Diagnostics;

namespace Pixelium.Core.Processors
{
    /// <summary>
    /// Base class for image processors that automatically tracks processing time.
    /// Derived classes should implement ProcessInternal() instead of Process().
    /// </summary>
    public abstract class ProcessorBase : IImageProcessor
    {
        public long ProcessingTimeMs { get; private set; }

        public bool Process(SKBitmap bitmap)
        {
            var stopwatch = Stopwatch.StartNew();
            bool result = ProcessInternal(bitmap);
            stopwatch.Stop();
            ProcessingTimeMs = stopwatch.ElapsedMilliseconds;
            return result;
        }

        /// <summary>
        /// Implement this method with your actual filter logic.
        /// Processing time will be automatically tracked.
        /// </summary>
        protected abstract bool ProcessInternal(SKBitmap bitmap);
    }
}

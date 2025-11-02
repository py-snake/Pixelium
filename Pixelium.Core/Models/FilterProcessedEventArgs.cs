using System;

namespace Pixelium.Core.Models
{
    public class FilterProcessedEventArgs : EventArgs
    {
        public string FilterName { get; set; } = string.Empty;

        /// <summary>
        /// Pure filter algorithm execution time (excludes backup, cloning, overhead)
        /// </summary>
        public long ProcessingTimeMs { get; set; }

        /// <summary>
        /// Total operation time from start to finish (includes all overhead)
        /// </summary>
        public long TotalTimeMs { get; set; }
    }
}

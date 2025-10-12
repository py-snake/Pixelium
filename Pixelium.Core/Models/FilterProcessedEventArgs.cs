using System;

namespace Pixelium.Core.Models
{
    public class FilterProcessedEventArgs : EventArgs
    {
        public string FilterName { get; set; } = string.Empty;
        public long ProcessingTimeMs { get; set; }
    }
}

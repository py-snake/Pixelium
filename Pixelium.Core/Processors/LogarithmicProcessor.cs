using SkiaSharp;
using Pixelium.Core.Services.LUT;
using System;

namespace Pixelium.Core.Processors
{
    public class LogarithmicProcessor : LutProcessorBase
    {
        private readonly double _c;

        public LogarithmicProcessor(ILookupTableService lutService, double c = 1.0) : base(lutService)
        {
            _c = c;
        }

        protected override byte[] GetLookupTable()
        {
            // Create logarithmic transformation LUT
            var table = new byte[256];
            double maxLog = Math.Log(1 + 255);

            for (int i = 0; i < 256; i++)
            {
                // s = c * log(1 + r)
                double result = _c * Math.Log(1 + i) * 255 / maxLog;
                table[i] = (byte)Math.Min(255, Math.Max(0, result));
            }

            return table;
        }
    }
}

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
            return _lutService.GetLogarithmicTable(_c);
        }
    }
}
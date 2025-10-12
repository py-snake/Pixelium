using SkiaSharp;
using Pixelium.Core.Services.LUT;
using Pixelium.Core.Processors;

namespace Pixelium.Core.Processors
{
    public class GammaProcessor : LutProcessorBase
    {
        private readonly double _gamma;

        public GammaProcessor(ILookupTableService lutService, double gamma) : base(lutService)
        {
            _gamma = gamma;
        }

        protected override byte[] GetLookupTable()
        {
            return _lutService.GetGammaTable(_gamma);
        }
    }
}

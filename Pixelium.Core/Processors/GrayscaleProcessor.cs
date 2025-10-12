using SkiaSharp;
using Pixelium.Core.Services.LUT;
using Pixelium.Core.Processors;

namespace Pixelium.Core.Processors
{
    public class GrayscaleProcessor : MultiChannelLutProcessor
    {
        public GrayscaleProcessor(ILookupTableService lutService) : base(lutService)
        {
        }

        protected override byte[] GetRedLookupTable()
        {
            return _lutService.GetGrayscaleRedTable();
        }

        protected override byte[] GetGreenLookupTable()
        {
            return _lutService.GetGrayscaleGreenTable();
        }

        protected override byte[] GetBlueLookupTable()
        {
            return _lutService.GetGrayscaleBlueTable();
        }
    }
}

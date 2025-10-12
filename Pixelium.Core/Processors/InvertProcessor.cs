using SkiaSharp;
using Pixelium.Core.Services.LUT;
using Pixelium.Core.Processors;  // Ensure this using is present

namespace Pixelium.Core.Processors
{
    public class InvertProcessor : LutProcessorBase
    {
        public InvertProcessor(ILookupTableService lutService) : base(lutService)
        {
        }

        protected override byte[] GetLookupTable()
        {
            return _lutService.GetInvertTable();
        }
    }
}

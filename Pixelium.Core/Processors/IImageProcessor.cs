using SkiaSharp;

namespace Pixelium.Core.Processors
{
    public interface IImageProcessor
    {
        bool Process(SKBitmap bitmap);
    }
}

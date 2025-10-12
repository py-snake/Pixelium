using System;

namespace Pixelium.Core.Services.LUT
{
    public interface ILookupTableService
    {
        byte[] GetInvertTable();
        byte[] GetGrayscaleRedTable();
        byte[] GetGrayscaleGreenTable();
        byte[] GetGrayscaleBlueTable();
        byte[] GetBrightnessTable(float factor);
        byte[] GetTable(string name);
        byte[] GetGammaTable(double gamma);

        bool IsInitialized { get; }
        int TableCount { get; }
    }
}

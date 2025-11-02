using SkiaSharp;
using System;
using System.Threading.Tasks;

namespace Pixelium.Core.Processors
{
    public class HistogramData
    {
        public int[] Red { get; set; } = new int[256];
        public int[] Green { get; set; } = new int[256];
        public int[] Blue { get; set; } = new int[256];
        public int[] Luminosity { get; set; } = new int[256];
        public int TotalPixels { get; set; }
    }

    public class HistogramProcessor
    {
        public static unsafe HistogramData CalculateHistogram(SKBitmap bitmap)
        {
            if (bitmap == null || bitmap.ColorType != SKColorType.Bgra8888)
                throw new ArgumentException("Invalid bitmap format");

            var histogram = new HistogramData
            {
                TotalPixels = bitmap.Width * bitmap.Height
            };

            var pixels = bitmap.GetPixels();
            var ptr = (byte*)pixels.ToPointer();

            Parallel.For(0, bitmap.Height, () => new HistogramData(),
                (y, loop, localHist) =>
                {
                    byte* rowPtr = ptr + (y * bitmap.Width * 4);

                    for (int x = 0; x < bitmap.Width * 4; x += 4)
                    {
                        byte b = rowPtr[x];
                        byte g = rowPtr[x + 1];
                        byte r = rowPtr[x + 2];

                        localHist.Blue[b]++;
                        localHist.Green[g]++;
                        localHist.Red[r]++;

                        int lum = (int)(0.299 * r + 0.587 * g + 0.114 * b);
                        localHist.Luminosity[lum]++;
                    }

                    return localHist;
                },
                localHist =>
                {
                    lock (histogram)
                    {
                        for (int i = 0; i < 256; i++)
                        {
                            histogram.Red[i] += localHist.Red[i];
                            histogram.Green[i] += localHist.Green[i];
                            histogram.Blue[i] += localHist.Blue[i];
                            histogram.Luminosity[i] += localHist.Luminosity[i];
                        }
                    }
                });

            return histogram;
        }
    }

    public class HistogramEqualizationProcessor : IImageProcessor
    {
        public long ProcessingTimeMs { get; private set; }

        public unsafe bool Process(SKBitmap bitmap)
        {
            if (bitmap == null || bitmap.ColorType != SKColorType.Bgra8888)
                return false;

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            var histogram = HistogramProcessor.CalculateHistogram(bitmap);
            int totalPixels = histogram.TotalPixels;

            // Equalize luminosity only to preserve colors better
            byte[] lumMap = CalculateEqualizationMap(histogram.Luminosity, totalPixels);

            var pixels = bitmap.GetPixels();
            var ptr = (byte*)pixels.ToPointer();

            Parallel.For(0, bitmap.Height, y =>
            {
                byte* rowPtr = ptr + (y * bitmap.Width * 4);

                for (int x = 0; x < bitmap.Width * 4; x += 4)
                {
                    byte b = rowPtr[x];
                    byte g = rowPtr[x + 1];
                    byte r = rowPtr[x + 2];

                    // Calculate original luminosity
                    int oldLum = (int)(0.299 * r + 0.587 * g + 0.114 * b);

                    // Get equalized luminosity
                    int newLum = lumMap[oldLum];

                    // Avoid division by zero
                    if (oldLum == 0)
                    {
                        rowPtr[x] = (byte)newLum;
                        rowPtr[x + 1] = (byte)newLum;
                        rowPtr[x + 2] = (byte)newLum;
                    }
                    else
                    {
                        // Scale RGB by luminosity change to preserve color ratios
                        double scale = newLum / (double)oldLum;
                        rowPtr[x] = (byte)Math.Min(255, Math.Max(0, b * scale));
                        rowPtr[x + 1] = (byte)Math.Min(255, Math.Max(0, g * scale));
                        rowPtr[x + 2] = (byte)Math.Min(255, Math.Max(0, r * scale));
                    }
                }
            });

            stopwatch.Stop();
            ProcessingTimeMs = stopwatch.ElapsedMilliseconds;

            return true;
        }

        private byte[] CalculateEqualizationMap(int[] histogram, int totalPixels)
        {
            double[] cdf = new double[256];
            cdf[0] = histogram[0] / (double)totalPixels;

            for (int i = 1; i < 256; i++)
            {
                cdf[i] = cdf[i - 1] + (histogram[i] / (double)totalPixels);
            }

            byte[] map = new byte[256];
            for (int i = 0; i < 256; i++)
            {
                map[i] = (byte)Math.Round(cdf[i] * 255);
            }

            return map;
        }
    }
}
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SkiaSharp;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: Pixelium.CLI <intensity-file.txt>");
            Console.WriteLine("Example: Pixelium.CLI intensity.txt");
            return;
        }

        string inputFile = args[0];

        try
        {
            const int levels = 8;

            // Read grayscale intensity data from file
            byte[,] intensityData = ReadIntensityData(inputFile, levels);

            // Create grayscale image from intensity data
            CreateGrayscaleImage(intensityData, "intensity_image.png", levels);

            // Calculate histogram using multi-threading
            int[] histogram = CalculateHistogramMultiThreaded(intensityData, levels);

            // Create histogram visualization
            CreateHistogramImage(histogram, "intensity_histogram.png", levels);

            // Display results
            PrintHistogram(histogram);

            Console.WriteLine("\n" + new string('=', 50));
            Console.WriteLine("APPLYING HISTOGRAM EQUALIZATION");
            Console.WriteLine(new string('=', 50));

            // Apply histogram equalization (creates new data, doesn't modify original)
            byte[,] equalizedData = ApplyHistogramEqualization(intensityData, levels);

            // Create grayscale image from equalized data
            CreateGrayscaleImage(equalizedData, "equalized_image.png", levels);

            // Calculate histogram for equalized image
            int[] equalizedHistogram = CalculateHistogramMultiThreaded(equalizedData, levels);

            // Create histogram visualization for equalized image
            CreateHistogramImage(equalizedHistogram, "equalized_histogram.png", levels);

            // Display results for equalized image
            PrintHistogram(equalizedHistogram);
            
            
            Console.WriteLine("Grayscale intensity processing completed!");
            Console.WriteLine($"Generated: intensity_image.png, intensity_histogram.png");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }

    static void PrintArray(int[] array)
    {
        for (int i = 0; i < array.Length; i++)
        {
            Console.Write($"{array[i]} ");
        }
        Console.WriteLine();
    }

    static byte[,] ReadIntensityData(string filePath, int levels)
    {
        var lines = File.ReadAllLines(filePath);
        int rows = lines.Length;
        int cols = lines[0].Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

        byte[,] data = new byte[rows, cols];

        Parallel.For(0, rows, i =>
        {
            var values = lines[i].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            for (int j = 0; j < cols; j++)
            {
                data[i, j] = byte.Parse(values[j]);
            }
        });

        Console.WriteLine($"Loaded {rows}x{cols} grayscale intensity data (0-{levels - 1})");
        return data;
    }

    static void CreateGrayscaleImage(byte[,] intensityData, string outputPath, int levels)
    {
        int height = intensityData.GetLength(0);
        int width = intensityData.GetLength(1);

        using var bitmap = new SKBitmap(width, height, SKColorType.Gray8, SKAlphaType.Premul);

        // Pre-calculate grayscale values (regular array, not ref local)
        byte[] intensityToGray = new byte[levels];
        for (int i = 0; i < levels; i++)
        {
            intensityToGray[i] = (byte)(i * 255 / (levels - 1));
        }

        // Get direct access to bitmap memory
        using var pixmap = bitmap.PeekPixels();
        var ptr = pixmap.GetPixels();

        // Parallel processing by rows
        Parallel.For(0, height, i =>
        {
            // Calculate the row pointer once per thread
            IntPtr rowPtr = ptr + i * pixmap.RowBytes;

            for (int j = 0; j < width; j++)
            {
                byte intensity = intensityData[i, j];
                byte grayValue = intensityToGray[intensity];

                unsafe
                {
                    byte* pixelPtr = (byte*)rowPtr.ToPointer() + j;
                    *pixelPtr = grayValue;
                }
            }
        });

        // Save the bitmap (inline the SaveBitmap method)
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.OpenWrite(outputPath);
        data.SaveTo(stream);

        Console.WriteLine($"Created grayscale image: {outputPath} ({width}x{height})");
    }

    static int[] CalculateHistogramMultiThreaded(byte[,] intensityData, int levels)
    {
        int rows = intensityData.GetLength(0);
        int cols = intensityData.GetLength(1);
        int[] globalHistogram = new int[levels]; // 0-7 intensity values

        // Use thread-local histograms to avoid locking
        Parallel.For(0, rows, () => new int[levels], (i, loop, localHistogram) =>
        {
            for (int j = 0; j < cols; j++)
            {
                localHistogram[intensityData[i, j]]++;
            }
            return localHistogram;
        },
        localHistogram =>
        {
            // Merge local histogram into global histogram
            lock (globalHistogram)
            {
                for (int k = 0; k < levels; k++)
                {
                    globalHistogram[k] += localHistogram[k];
                }
            }
        });
        PrintArray(globalHistogram);
        return globalHistogram;
    }

    static void CreateHistogramImage(int[] histogram, string outputPath, int levels)
    {
        // Calculate dimensions based on levels
        int barWidth = 30;
        int width = levels * barWidth;
        int textMargin = 40; // Space for text below bars
        int topMargin = 20; // Small margin at top

        // Calculate dynamic height based on maximum value
        int maxValue = histogram.Max();

        // Ensure reasonable minimum and maximum heights
        int dynamicHeight = Math.Max((maxValue > 0 ? maxValue : 1) + textMargin + topMargin, 200);
        int height = Math.Min(dynamicHeight, 2000); // Cap at 2000px to prevent huge images

        using var bitmap = new SKBitmap(width, height, SKColorType.Gray8, SKAlphaType.Premul);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.White);

        // Calculate scale factor based on dynamic height
        float scaleFactor = (height - textMargin - topMargin) / (float)Math.Max(maxValue, 1);

        using var barPaint = new SKPaint
        {
            Color = SKColors.Black,
            Style = SKPaintStyle.Fill,
            IsAntialias = false
        };

        using var textPaint = new SKPaint
        {
            Color = SKColors.Black,
            TextSize = 11,
            IsAntialias = true,
            TextAlign = SKTextAlign.Center
        };

        // Draw bars and labels
        for (int i = 0; i < histogram.Length; i++)
        {
            int barHeight = (int)(histogram[i] * scaleFactor);
            int x = i * barWidth;
            int y = height - textMargin - barHeight;

            // Ensure minimum bar height for visibility (if value > 0)
            if (histogram[i] > 0 && barHeight < 1)
            {
                barHeight = 1;
                y = height - textMargin - 1;
            }

            // Draw bar (only if height > 0)
            if (barHeight > 0)
            {
                var barRect = new SKRect(x, y, x + barWidth, height - textMargin);
                canvas.DrawRect(barRect, barPaint);
            }

            // Draw intensity value at bottom (always show)
            canvas.DrawText(i.ToString(), x + barWidth / 2, height - 25, textPaint);

            // Draw count value below intensity (always show, even if 0)
            canvas.DrawText(histogram[i].ToString(), x + barWidth / 2, height - 10, textPaint);
        }

        // Save the image
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.OpenWrite(outputPath);
        data.SaveTo(stream);

        Console.WriteLine($"Created histogram image: {outputPath} ({width}x{height}) - Max value: {maxValue}");
    }


    static int[] CalculateHistogram(byte[,] intensityData, int levels)
    {
        int rows = intensityData.GetLength(0);
        int cols = intensityData.GetLength(1);
        int[] histogram = new int[levels];

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                histogram[intensityData[i, j]]++;
            }
        }

        return histogram;
    }

    static byte[,] ApplyHistogramEqualization(byte[,] intensityData, int levels)
    {
        int height = intensityData.GetLength(0);
        int width = intensityData.GetLength(1);
        int totalPixels = height * width;

        Console.WriteLine($"\nStarting histogram equalization:");
        Console.WriteLine($"Image size: {width}x{height}, Total pixels: {totalPixels}");

        // Calculate histogram of original image
        int[] histogram = CalculateHistogram(intensityData, levels);

        Console.WriteLine("Original histogram:");
        for (int i = 0; i < levels; i++)
        {
            Console.WriteLine($"  Intensity {i}: {histogram[i]} pixels ({(histogram[i] * 100.0 / totalPixels):F1}%)");
        }

        // Calculate probability density function (PDF)
        double[] pdf = new double[levels];
        for (int i = 0; i < levels; i++)
        {
            pdf[i] = histogram[i] / (double)totalPixels;
        }

        // Calculate cumulative distribution function (CDF)
        double[] cdf = new double[levels];
        cdf[0] = pdf[0];
        for (int i = 1; i < levels; i++)
        {
            cdf[i] = cdf[i - 1] + pdf[i];
        }

        Console.WriteLine("Cumulative Distribution Function (CDF):");
        for (int i = 0; i < levels; i++)
        {
            Console.WriteLine($"  Intensity {i}: cdf = {cdf[i]:F4}");
        }

        // Create equalization mapping
        byte[] equalizationMap = new byte[levels];
        for (int i = 0; i < levels; i++)
        {
            // Standard formula: s = T(r) = (L-1) * CDF(r)
            double equalizedValue = cdf[i] * (levels - 1);
            equalizationMap[i] = (byte)Math.Round(equalizedValue);
            Console.WriteLine($"  Intensity {i}: cdf={cdf[i]:F4}, equalized={equalizedValue:F2} -> maps to {equalizationMap[i]}");
        }

        // Apply equalization
        byte[,] equalizedData = new byte[height, width];
        int[] equalizedHistogram = new int[levels];

        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                byte originalIntensity = intensityData[i, j];
                byte newIntensity = equalizationMap[originalIntensity];
                equalizedData[i, j] = newIntensity;
                equalizedHistogram[newIntensity]++;
            }
        }

        Console.WriteLine("\nEqualized histogram:");
        for (int i = 0; i < levels; i++)
        {
            Console.WriteLine($"  Intensity {i}: {equalizedHistogram[i]} pixels ({(equalizedHistogram[i] * 100.0 / totalPixels):F1}%)");
        }

        return equalizedData;
    }

    static void PrintHistogram(int[] histogram)
    {
        Console.WriteLine("\nGrayscale Intensity Histogram:");
        Console.WriteLine("==============================");
        int totalPixels = 0;
        for (int i = 0; i < histogram.Length; i++)
        {
            Console.WriteLine($"Intensity {i}: {histogram[i],8} pixels ({CalculatePercentage(histogram[i], histogram.Sum()),6:F2}%)");
            totalPixels += histogram[i];
        }
        Console.WriteLine($"Total pixels: {totalPixels}");

        // Calculate and display statistics
        double mean = CalculateMean(histogram);
        int mode = CalculateMode(histogram);
        Console.WriteLine($"Mean intensity: {mean:F2}");
        Console.WriteLine($"Mode intensity: {mode}");
    }

    static double CalculatePercentage(int value, int total)
    {
        return total == 0 ? 0 : (value * 100.0) / total;
    }

    static double CalculateMean(int[] histogram)
    {
        double sum = 0;
        int total = 0;
        for (int i = 0; i < histogram.Length; i++)
        {
            sum += i * histogram[i];
            total += histogram[i];
        }
        return total == 0 ? 0 : sum / total;
    }

    static int CalculateMode(int[] histogram)
    {
        int maxIndex = 0;
        for (int i = 1; i < histogram.Length; i++)
        {
            if (histogram[i] > histogram[maxIndex])
            {
                maxIndex = i;
            }
        }
        return maxIndex;
    }
}

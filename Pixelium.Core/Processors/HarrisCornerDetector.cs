using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Pixelium.Core.Processors
{
    /// <summary>
    /// Harris Corner Detector - detects keypoints/features in images
    /// Algorithm:
    /// 1. Calculate image gradients (Sobel)
    /// 2. Compute structure tensor components (Ix², Iy², IxIy)
    /// 3. Apply Gaussian smoothing to tensor components
    /// 4. Calculate corner response: R = det(M) - k*trace(M)²
    /// 5. Non-maximum suppression
    /// 6. Threshold and mark corners
    /// </summary>
    public class HarrisCornerDetector : IImageProcessor
    {
        private readonly float _threshold;
        private readonly float _k;
        private readonly float _sigma;
        private readonly int _suppressionRadius;
        private readonly SKColor _markerColor;
        private readonly int _markerSize;

        /// <summary>
        /// Creates a Harris corner detector
        /// </summary>
        /// <param name="threshold">Corner response threshold (0.01-0.1, typical: 0.01)</param>
        /// <param name="k">Harris parameter (0.04-0.06, typical: 0.04)</param>
        /// <param name="sigma">Gaussian smoothing sigma (typical: 1.0)</param>
        /// <param name="suppressionRadius">Non-maximum suppression radius (typical: 3)</param>
        /// <param name="markerColor">Color for marking detected corners</param>
        /// <param name="markerSize">Size of corner markers in pixels (typical: 5)</param>
        public HarrisCornerDetector(
            float threshold = 0.01f,
            float k = 0.04f,
            float sigma = 1.0f,
            int suppressionRadius = 3,
            SKColor? markerColor = null,
            int markerSize = 5)
        {
            _threshold = threshold;
            _k = k;
            _sigma = sigma;
            _suppressionRadius = suppressionRadius;
            _markerColor = markerColor ?? SKColors.Red;
            _markerSize = markerSize;
        }

        public unsafe bool Process(SKBitmap bitmap)
        {
            if (bitmap == null || bitmap.ColorType != SKColorType.Bgra8888)
                return false;

            int width = bitmap.Width;
            int height = bitmap.Height;

            // Step 1: Convert to grayscale for gradient calculation
            float[,] gray = new float[height, width];
            var pixels = bitmap.GetPixels();
            var ptr = (byte*)pixels.ToPointer();

            Parallel.For(0, height, y =>
            {
                byte* rowPtr = ptr + (y * width * 4);
                for (int x = 0; x < width; x++)
                {
                    byte b = rowPtr[x * 4];
                    byte g = rowPtr[x * 4 + 1];
                    byte r = rowPtr[x * 4 + 2];
                    gray[y, x] = 0.299f * r + 0.587f * g + 0.114f * b;
                }
            });

            // Step 2: Calculate gradients using Sobel operator
            float[,] Ix = new float[height, width];
            float[,] Iy = new float[height, width];

            Parallel.For(1, height - 1, y =>
            {
                for (int x = 1; x < width - 1; x++)
                {
                    // Sobel X kernel
                    float gx = -gray[y - 1, x - 1] + gray[y - 1, x + 1]
                             - 2 * gray[y, x - 1] + 2 * gray[y, x + 1]
                             - gray[y + 1, x - 1] + gray[y + 1, x + 1];

                    // Sobel Y kernel
                    float gy = -gray[y - 1, x - 1] - 2 * gray[y - 1, x] - gray[y - 1, x + 1]
                             + gray[y + 1, x - 1] + 2 * gray[y + 1, x] + gray[y + 1, x + 1];

                    Ix[y, x] = gx;
                    Iy[y, x] = gy;
                }
            });

            // Step 3: Compute structure tensor components
            float[,] Ixx = new float[height, width];
            float[,] Iyy = new float[height, width];
            float[,] Ixy = new float[height, width];

            Parallel.For(0, height, y =>
            {
                for (int x = 0; x < width; x++)
                {
                    Ixx[y, x] = Ix[y, x] * Ix[y, x];
                    Iyy[y, x] = Iy[y, x] * Iy[y, x];
                    Ixy[y, x] = Ix[y, x] * Iy[y, x];
                }
            });

            // Step 4: Apply Gaussian smoothing to structure tensor
            var gaussian = CreateGaussianKernel(_sigma);
            Ixx = ApplyGaussianSmoothing(Ixx, gaussian);
            Iyy = ApplyGaussianSmoothing(Iyy, gaussian);
            Ixy = ApplyGaussianSmoothing(Ixy, gaussian);

            // Step 5: Calculate Harris corner response
            float[,] cornerResponse = new float[height, width];
            float maxResponse = 0;

            Parallel.For(0, height, () => 0f, (y, loop, localMax) =>
            {
                for (int x = 0; x < width; x++)
                {
                    // Harris corner response: R = det(M) - k*trace(M)²
                    // M = [ Ixx  Ixy ]
                    //     [ Ixy  Iyy ]
                    float det = Ixx[y, x] * Iyy[y, x] - Ixy[y, x] * Ixy[y, x];
                    float trace = Ixx[y, x] + Iyy[y, x];
                    float response = det - _k * trace * trace;

                    cornerResponse[y, x] = response;
                    if (response > localMax)
                        localMax = response;
                }
                return localMax;
            },
            localMax =>
            {
                lock (cornerResponse)
                {
                    if (localMax > maxResponse)
                        maxResponse = localMax;
                }
            });

            // Step 6: Non-maximum suppression and thresholding
            float responseThreshold = maxResponse * _threshold;
            List<(int x, int y, float response)> corners = new List<(int, int, float)>();

            for (int y = _suppressionRadius; y < height - _suppressionRadius; y++)
            {
                for (int x = _suppressionRadius; x < width - _suppressionRadius; x++)
                {
                    float response = cornerResponse[y, x];

                    if (response < responseThreshold)
                        continue;

                    // Check if this is a local maximum
                    bool isLocalMax = true;
                    for (int dy = -_suppressionRadius; dy <= _suppressionRadius && isLocalMax; dy++)
                    {
                        for (int dx = -_suppressionRadius; dx <= _suppressionRadius; dx++)
                        {
                            if (dx == 0 && dy == 0)
                                continue;

                            if (cornerResponse[y + dy, x + dx] > response)
                            {
                                isLocalMax = false;
                                break;
                            }
                        }
                    }

                    if (isLocalMax)
                    {
                        corners.Add((x, y, response));
                    }
                }
            }

            // Step 7: Draw corner markers on the image
            DrawCornerMarkers(bitmap, corners);

            return true;
        }

        private float[,] CreateGaussianKernel(float sigma)
        {
            int size = (int)(6 * sigma + 1);
            if (size % 2 == 0) size++;
            int radius = size / 2;

            float[,] kernel = new float[size, size];
            float sum = 0;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int dx = x - radius;
                    int dy = y - radius;
                    float value = (float)Math.Exp(-(dx * dx + dy * dy) / (2 * sigma * sigma));
                    kernel[y, x] = value;
                    sum += value;
                }
            }

            // Normalize
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    kernel[y, x] /= sum;

            return kernel;
        }

        private float[,] ApplyGaussianSmoothing(float[,] input, float[,] kernel)
        {
            int height = input.GetLength(0);
            int width = input.GetLength(1);
            int kSize = kernel.GetLength(0);
            int kRadius = kSize / 2;

            float[,] output = new float[height, width];

            Parallel.For(kRadius, height - kRadius, y =>
            {
                for (int x = kRadius; x < width - kRadius; x++)
                {
                    float sum = 0;

                    for (int ky = 0; ky < kSize; ky++)
                    {
                        for (int kx = 0; kx < kSize; kx++)
                        {
                            int py = y + ky - kRadius;
                            int px = x + kx - kRadius;
                            sum += input[py, px] * kernel[ky, kx];
                        }
                    }

                    output[y, x] = sum;
                }
            });

            return output;
        }

        private unsafe void DrawCornerMarkers(SKBitmap bitmap, List<(int x, int y, float response)> corners)
        {
            var pixels = bitmap.GetPixels();
            var ptr = (byte*)pixels.ToPointer();
            int width = bitmap.Width;
            int height = bitmap.Height;

            byte markerB = _markerColor.Blue;
            byte markerG = _markerColor.Green;
            byte markerR = _markerColor.Red;
            byte markerA = _markerColor.Alpha;

            foreach (var corner in corners)
            {
                // Draw a cross at each corner
                for (int i = -_markerSize; i <= _markerSize; i++)
                {
                    // Horizontal line
                    int hx = corner.x + i;
                    int hy = corner.y;
                    if (hx >= 0 && hx < width && hy >= 0 && hy < height)
                    {
                        int offset = (hy * width + hx) * 4;
                        ptr[offset] = markerB;
                        ptr[offset + 1] = markerG;
                        ptr[offset + 2] = markerR;
                        ptr[offset + 3] = markerA;
                    }

                    // Vertical line
                    int vx = corner.x;
                    int vy = corner.y + i;
                    if (vx >= 0 && vx < width && vy >= 0 && vy < height)
                    {
                        int offset = (vy * width + vx) * 4;
                        ptr[offset] = markerB;
                        ptr[offset + 1] = markerG;
                        ptr[offset + 2] = markerR;
                        ptr[offset + 3] = markerA;
                    }
                }

                // Draw a small circle around the corner
                for (int dy = -_markerSize; dy <= _markerSize; dy++)
                {
                    for (int dx = -_markerSize; dx <= _markerSize; dx++)
                    {
                        if (dx * dx + dy * dy <= (_markerSize / 2) * (_markerSize / 2))
                        {
                            int cx = corner.x + dx;
                            int cy = corner.y + dy;
                            if (cx >= 0 && cx < width && cy >= 0 && cy < height)
                            {
                                int offset = (cy * width + cx) * 4;
                                ptr[offset] = markerB;
                                ptr[offset + 1] = markerG;
                                ptr[offset + 2] = markerR;
                                ptr[offset + 3] = markerA;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Returns the number of corners detected in the last processing
        /// (This would be stored in a field in a more complete implementation)
        /// </summary>
        public int DetectedCornerCount { get; private set; }
    }
}

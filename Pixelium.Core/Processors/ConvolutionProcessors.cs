using SkiaSharp;
using System;
using System.Threading.Tasks;

namespace Pixelium.Core.Processors
{
    public abstract class ConvolutionProcessor : IImageProcessor
    {
        protected abstract float[,] GetKernel();
        protected virtual float KernelDivisor => 1.0f;
        protected virtual float KernelBias => 0.0f;

        public unsafe bool Process(SKBitmap bitmap)
        {
            if (bitmap == null || bitmap.ColorType != SKColorType.Bgra8888)
                return false;

            var kernel = GetKernel();
            int kSize = kernel.GetLength(0);
            int kRadius = kSize / 2;
            float divisor = KernelDivisor;
            float bias = KernelBias;

            var temp = bitmap.Copy();
            if (temp == null) return false;

            var srcPixels = temp.GetPixels();
            var dstPixels = bitmap.GetPixels();
            var srcPtr = (byte*)srcPixels.ToPointer();
            var dstPtr = (byte*)dstPixels.ToPointer();

            int width = bitmap.Width;
            int height = bitmap.Height;

            Parallel.For(0, height, y =>
            {
                for (int x = 0; x < width; x++)
                {
                    float sumR = 0, sumG = 0, sumB = 0;

                    for (int ky = 0; ky < kSize; ky++)
                    {
                        for (int kx = 0; kx < kSize; kx++)
                        {
                            int px = Math.Clamp(x + kx - kRadius, 0, width - 1);
                            int py = Math.Clamp(y + ky - kRadius, 0, height - 1);

                            int offset = (py * width + px) * 4;
                            float kernelValue = kernel[ky, kx];

                            sumB += srcPtr[offset] * kernelValue;
                            sumG += srcPtr[offset + 1] * kernelValue;
                            sumR += srcPtr[offset + 2] * kernelValue;
                        }
                    }

                    int dstOffset = (y * width + x) * 4;
                    dstPtr[dstOffset] = (byte)Math.Clamp(sumB / divisor + bias, 0, 255);
                    dstPtr[dstOffset + 1] = (byte)Math.Clamp(sumG / divisor + bias, 0, 255);
                    dstPtr[dstOffset + 2] = (byte)Math.Clamp(sumR / divisor + bias, 0, 255);
                    dstPtr[dstOffset + 3] = srcPtr[dstOffset + 3];
                }
            });

            temp.Dispose();
            return true;
        }
    }

    public class BoxFilterProcessor : ConvolutionProcessor
    {
        private readonly int _size;

        public BoxFilterProcessor(int size = 3)
        {
            _size = size;
        }

        protected override float[,] GetKernel()
        {
            var kernel = new float[_size, _size];
            for (int i = 0; i < _size; i++)
                for (int j = 0; j < _size; j++)
                    kernel[i, j] = 1.0f;
            return kernel;
        }

        protected override float KernelDivisor => _size * _size;
    }

    public class GaussianFilterProcessor : ConvolutionProcessor
    {
        private readonly float _sigma;
        private readonly int _size;

        public GaussianFilterProcessor(float sigma = 1.0f, int size = 5)
        {
            _sigma = sigma;
            _size = size;
        }

        protected override float[,] GetKernel()
        {
            var kernel = new float[_size, _size];
            int radius = _size / 2;
            float sum = 0;

            for (int y = 0; y < _size; y++)
            {
                for (int x = 0; x < _size; x++)
                {
                    int dx = x - radius;
                    int dy = y - radius;
                    float value = (float)(Math.Exp(-(dx * dx + dy * dy) / (2 * _sigma * _sigma))
                                          / (2 * Math.PI * _sigma * _sigma));
                    kernel[y, x] = value;
                    sum += value;
                }
            }

            for (int y = 0; y < _size; y++)
                for (int x = 0; x < _size; x++)
                    kernel[y, x] /= sum;

            return kernel;
        }
    }

    public class SobelEdgeDetector : IImageProcessor
    {
        public unsafe bool Process(SKBitmap bitmap)
        {
            if (bitmap == null || bitmap.ColorType != SKColorType.Bgra8888)
                return false;

            float[,] sobelX = new float[,] {
                { -1, 0, 1 },
                { -2, 0, 2 },
                { -1, 0, 1 }
            };

            float[,] sobelY = new float[,] {
                { -1, -2, -1 },
                {  0,  0,  0 },
                {  1,  2,  1 }
            };

            var temp = bitmap.Copy();
            if (temp == null) return false;

            var srcPixels = temp.GetPixels();
            var dstPixels = bitmap.GetPixels();
            var srcPtr = (byte*)srcPixels.ToPointer();
            var dstPtr = (byte*)dstPixels.ToPointer();

            int width = bitmap.Width;
            int height = bitmap.Height;

            Parallel.For(0, height, y =>
            {
                for (int x = 0; x < width; x++)
                {
                    float gxR = 0, gyR = 0;
                    float gxG = 0, gyG = 0;
                    float gxB = 0, gyB = 0;

                    for (int ky = 0; ky < 3; ky++)
                    {
                        for (int kx = 0; kx < 3; kx++)
                        {
                            int px = Math.Clamp(x + kx - 1, 0, width - 1);
                            int py = Math.Clamp(y + ky - 1, 0, height - 1);
                            int offset = (py * width + px) * 4;

                            byte b = srcPtr[offset];
                            byte g = srcPtr[offset + 1];
                            byte r = srcPtr[offset + 2];

                            gxB += b * sobelX[ky, kx];
                            gyB += b * sobelY[ky, kx];
                            gxG += g * sobelX[ky, kx];
                            gyG += g * sobelY[ky, kx];
                            gxR += r * sobelX[ky, kx];
                            gyR += r * sobelY[ky, kx];
                        }
                    }

                    float magR = (float)Math.Sqrt(gxR * gxR + gyR * gyR);
                    float magG = (float)Math.Sqrt(gxG * gxG + gyG * gyG);
                    float magB = (float)Math.Sqrt(gxB * gxB + gyB * gyB);

                    int dstOffset = (y * width + x) * 4;
                    dstPtr[dstOffset] = (byte)Math.Min(255, magB);
                    dstPtr[dstOffset + 1] = (byte)Math.Min(255, magG);
                    dstPtr[dstOffset + 2] = (byte)Math.Min(255, magR);
                    dstPtr[dstOffset + 3] = srcPtr[dstOffset + 3];
                }
            });

            temp.Dispose();
            return true;
        }
    }

    public class LaplaceEdgeDetector : ConvolutionProcessor
    {
        protected override float[,] GetKernel()
        {
            return new float[,] {
                {  0, -1,  0 },
                { -1,  4, -1 },
                {  0, -1,  0 }
            };
        }

        protected override float KernelBias => 128f;
    }
}

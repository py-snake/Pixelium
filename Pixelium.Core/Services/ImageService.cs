using Pixelium.Core.Commands;
using Pixelium.Core.Models;
using Pixelium.Core.Processors;
using Pixelium.Core.Services.LUT;
using SkiaSharp;
using System;
using System.Diagnostics;
using System.IO;

namespace Pixelium.Core.Services
{
    public class ImageService
    {
        private readonly CommandHistory _commandHistory;
        private readonly ILookupTableService _lutService;

        public Project? CurrentProject { get; private set; }

        public event EventHandler? ProjectChanged;
        public event EventHandler? LayerChanged;
        public event EventHandler<FilterProcessedEventArgs>? FilterProcessed;

        public bool CanUndo => _commandHistory.CanUndo;
        public bool CanRedo => _commandHistory.CanRedo;

        public ImageService()
        {
            _commandHistory = new CommandHistory();
            _lutService = new LookupTableService(); 
        }

        public void CreateNewProject(int width, int height, string name = "Untitled")
        {
            CurrentProject?.Dispose();
            CurrentProject = new Project(width, height) { Name = name };
            ProjectChanged?.Invoke(this, EventArgs.Empty);
            LayerChanged?.Invoke(this, EventArgs.Empty);
        }

        public bool LoadImage(string filePath)
        {
            try
            {
                using var stream = File.OpenRead(filePath);
                using var originalBitmap = SKBitmap.Decode(stream);

                if (originalBitmap == null) return false;

                var bitmap = new SKBitmap(originalBitmap.Width, originalBitmap.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
                using var canvas = new SKCanvas(bitmap);
                canvas.DrawBitmap(originalBitmap, 0, 0);

                CreateNewProject(bitmap.Width, bitmap.Height, Path.GetFileNameWithoutExtension(filePath));
                CurrentProject!.ActiveLayer!.Content.Dispose();
                CurrentProject.ActiveLayer.Content = bitmap;

                LayerChanged?.Invoke(this, EventArgs.Empty);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool SaveImage(string filePath)
        {
            if (CurrentProject == null) return false;

            try
            {
                using var flattened = CurrentProject.FlattenLayers();
                using var image = SKImage.FromBitmap(flattened);
                
                var extension = Path.GetExtension(filePath).ToLowerInvariant();
                var format = extension switch
                {
                    ".jpg" or ".jpeg" => SKEncodedImageFormat.Jpeg,
                    ".png" => SKEncodedImageFormat.Png,
                    ".bmp" => SKEncodedImageFormat.Bmp,
                    _ => SKEncodedImageFormat.Png
                };

                using var data = image.Encode(format, 100);
                using var fileStream = File.OpenWrite(filePath);
                data.SaveTo(fileStream);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void ApplyFilter(IImageProcessor processor, string filterName)
        {
            if (CurrentProject?.ActiveLayer?.Content == null || CurrentProject.ActiveLayer.IsLocked) 
                return;

            var stopwatch = Stopwatch.StartNew();
            var command = new FilterCommand(CurrentProject.ActiveLayer, processor, filterName);
            _commandHistory.Execute(command);
            stopwatch.Stop();

            FilterProcessed?.Invoke(this, new FilterProcessedEventArgs
            {
                FilterName = filterName,
                ProcessingTimeMs = stopwatch.ElapsedMilliseconds
            });

            LayerChanged?.Invoke(this, EventArgs.Empty);
        }

        public void ApplyGrayscale() => 
            ApplyFilter(new GrayscaleProcessor(_lutService), "Grayscale");

        public void ApplyInvert() => 
            ApplyFilter(new InvertProcessor(_lutService), "Invert");

        public void ApplyGamma(double gamma) => 
            ApplyFilter(new GammaProcessor(_lutService, gamma), $"Gamma {gamma:0.0}");

        public void ApplyLogarithmic(double c = 1.0) => 
            ApplyFilter(new LogarithmicProcessor(_lutService, c), $"Logarithmic {c:0.0}");

        public void ApplyFlip(FlipDirection direction) => 
            ApplyFilter(new FlipProcessor(direction), $"Flip {direction}");

        public HistogramData GetHistogram()
        {
            if (CurrentProject?.ActiveLayer?.Content == null) 
                return new HistogramData();

            return HistogramProcessor.CalculateHistogram(CurrentProject.ActiveLayer.Content);
        }

        public void ApplyHistogramEqualization() => 
            ApplyFilter(new HistogramEqualizationProcessor(), "Histogram Equalization");

        public void ApplyBoxFilter(int size = 3) => 
            ApplyFilter(new BoxFilterProcessor(size), $"Box Filter {size}x{size}");

        public void ApplyGaussianFilter(float sigma = 1.0f, int size = 5) => 
            ApplyFilter(new GaussianFilterProcessor(sigma, size), $"Gaussian Filter σ={sigma:0.0}");

        public void ApplySobelEdgeDetection() => 
            ApplyFilter(new SobelEdgeDetector(), "Sobel Edge Detection");

        public void ApplyLaplaceEdgeDetection() => 
            ApplyFilter(new LaplaceEdgeDetector(), "Laplace Edge Detection");

        public void AddNewLayer(string name = "New Layer")
        {
            if (CurrentProject == null) return;
            CurrentProject.AddLayer(name);
            LayerChanged?.Invoke(this, EventArgs.Empty);
        }

        public void RemoveActiveLayer()
        {
            if (CurrentProject?.ActiveLayer == null) return;
            CurrentProject.RemoveLayer(CurrentProject.ActiveLayer);
            LayerChanged?.Invoke(this, EventArgs.Empty);
        }

        public void DuplicateActiveLayer()
        {
            if (CurrentProject?.ActiveLayer == null) return;
            CurrentProject.DuplicateLayer(CurrentProject.ActiveLayer);
            LayerChanged?.Invoke(this, EventArgs.Empty);
        }

        public void MergeLayerDown()
        {
            if (CurrentProject?.ActiveLayer == null) return;
            var activeIndex = CurrentProject.Layers.IndexOf(CurrentProject.ActiveLayer);
            if (activeIndex <= 0) return;

            var upperLayer = CurrentProject.ActiveLayer;
            var lowerLayer = CurrentProject.Layers[activeIndex - 1];

            using var canvas = new SKCanvas(lowerLayer.Content);
            using var paint = new SKPaint
            {
                BlendMode = upperLayer.BlendMode,
                Color = SKColors.White.WithAlpha((byte)(upperLayer.Opacity * 255))
            };
            canvas.DrawBitmap(upperLayer.Content, 0, 0, paint);

            CurrentProject.RemoveLayer(upperLayer);
            CurrentProject.ActiveLayer = lowerLayer;
            LayerChanged?.Invoke(this, EventArgs.Empty);
        }

        public void SetActiveLayer(Layer layer)
        {
            if (CurrentProject == null) return;
            CurrentProject.ActiveLayer = layer;
            LayerChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Undo()
        {
            if (_commandHistory.Undo())
            {
                LayerChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public void Redo()
        {
            if (_commandHistory.Redo())
            {
                LayerChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
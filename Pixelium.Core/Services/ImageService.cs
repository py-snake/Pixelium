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
    public class ImageService : IDisposable
    {
        private readonly CommandHistory _commandHistory;
        private readonly ILookupTableService _lutService;
        private bool _disposed = false;

        public Project? CurrentProject { get; private set; }
        public EditMode CurrentEditMode { get; set; } = EditMode.NonDestructive;

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
            _commandHistory.Clear(); // Clear undo/redo history for new project
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

            var totalStopwatch = Stopwatch.StartNew();

            if (CurrentEditMode == EditMode.NonDestructive)
            {
                // Non-destructive: Create a new layer with the filter applied
                var newLayer = CurrentProject.ActiveLayer.Clone();
                newLayer.Name = $"{filterName}";

                processor.Process(newLayer.Content);

                int activeIndex = CurrentProject.Layers.IndexOf(CurrentProject.ActiveLayer);

                // Use command for undo/redo support
                var command = new AddLayerCommand(CurrentProject, newLayer, activeIndex + 1, filterName);
                _commandHistory.Execute(command);
            }
            else
            {
                // Destructive: Modify active layer in-place with undo support
                var command = new FilterCommand(CurrentProject.ActiveLayer, processor, filterName);
                _commandHistory.Execute(command);
            }

            totalStopwatch.Stop();

            // Report both pure processing time and total operation time
            FilterProcessed?.Invoke(this, new FilterProcessedEventArgs
            {
                FilterName = filterName,
                ProcessingTimeMs = processor.ProcessingTimeMs,
                TotalTimeMs = totalStopwatch.ElapsedMilliseconds
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

        public void ApplyHarrisCornerDetection(float threshold = 0.01f, float k = 0.04f, float sigma = 1.0f) =>
            ApplyFilter(new HarrisCornerDetector(threshold, k, sigma), $"Harris Corner Detection (t={threshold:0.000})");

        public void AddNewLayer(string name = "New Layer")
        {
            if (CurrentProject == null) return;

            var layer = new Layer(CurrentProject.Width, CurrentProject.Height, name);
            var command = new AddLayerCommand(CurrentProject, layer, CurrentProject.Layers.Count, "Add Layer");
            _commandHistory.Execute(command);

            LayerChanged?.Invoke(this, EventArgs.Empty);
        }

        public void RemoveActiveLayer()
        {
            if (CurrentProject?.ActiveLayer == null || CurrentProject.Layers.Count <= 1) return;

            var command = new RemoveLayerCommand(CurrentProject, CurrentProject.ActiveLayer);
            _commandHistory.Execute(command);

            LayerChanged?.Invoke(this, EventArgs.Empty);
        }

        public void DuplicateActiveLayer()
        {
            if (CurrentProject?.ActiveLayer == null) return;

            var duplicate = CurrentProject.ActiveLayer.Clone();
            duplicate.Name = $"{CurrentProject.ActiveLayer.Name} copy";
            int index = CurrentProject.Layers.IndexOf(CurrentProject.ActiveLayer);

            var command = new AddLayerCommand(CurrentProject, duplicate, index + 1, "Duplicate Layer");
            _commandHistory.Execute(command);

            LayerChanged?.Invoke(this, EventArgs.Empty);
        }

        public void MergeLayerDown()
        {
            if (CurrentProject?.ActiveLayer == null) return;
            var activeIndex = CurrentProject.Layers.IndexOf(CurrentProject.ActiveLayer);
            if (activeIndex <= 0) return;

            var upperLayer = CurrentProject.ActiveLayer;
            var lowerLayer = CurrentProject.Layers[activeIndex - 1];

            var command = new MergeLayerDownCommand(CurrentProject, upperLayer, lowerLayer);
            _commandHistory.Execute(command);

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

        public void Dispose()
        {
            if (!_disposed)
            {
                _commandHistory?.Dispose();
                CurrentProject?.Dispose();
                _disposed = true;
            }
        }
    }
}
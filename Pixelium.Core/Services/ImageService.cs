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
        public event EventHandler<FilterProcessedEventArgs>? FilterProcessed; // Új event

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
        }

        public bool LoadImage(string filePath)
        {
            try
            {
                using var stream = File.OpenRead(filePath);
                using var originalBitmap = SKBitmap.Decode(stream);

                if (originalBitmap == null) return false;

                // Convert to BGRA8888 for consistent processing
                var bitmap = new SKBitmap(originalBitmap.Width, originalBitmap.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
                using var canvas = new SKCanvas(bitmap);
                canvas.DrawBitmap(originalBitmap, 0, 0);

                CreateNewProject(bitmap.Width, bitmap.Height, Path.GetFileNameWithoutExtension(filePath));
                CurrentProject!.ActiveLayer.Content.Dispose();
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
            if (CurrentProject?.ActiveLayer?.Content == null) return false;

            try
            {
                using var image = SKImage.FromBitmap(CurrentProject.ActiveLayer.Content);
                using var data = image.Encode(SKEncodedImageFormat.Png, 100);
                using var stream = File.OpenWrite(filePath);
                data.SaveTo(stream);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void ApplyGrayscale()
        {
            if (CurrentProject?.ActiveLayer?.Content == null) return;

            // Stopper indítása
            var stopwatch = Stopwatch.StartNew();

            var processor = new GrayscaleProcessor(_lutService);
            var command = new FilterCommand(CurrentProject.ActiveLayer, processor, "Grayscale");

            _commandHistory.Execute(command);

            // Stopper leállítása és idő mérése
            stopwatch.Stop();
            var processingTime = stopwatch.ElapsedMilliseconds;

            // Event triggerelése az idővel
            FilterProcessed?.Invoke(this, new FilterProcessedEventArgs
            {
                FilterName = "Grayscale",
                ProcessingTimeMs = processingTime
            });

            LayerChanged?.Invoke(this, EventArgs.Empty);
        }

        public void ApplyInvert()
        {
            if (CurrentProject?.ActiveLayer?.Content == null) return;

            // Stopper indítása
            var stopwatch = Stopwatch.StartNew();
            
            var processor = new InvertProcessor(_lutService);
            var command = new FilterCommand(CurrentProject.ActiveLayer, processor, "Invert");

            _commandHistory.Execute(command);
            
            // Stopper leállítása és idő mérése
            stopwatch.Stop();
            var processingTime = stopwatch.ElapsedMilliseconds;
            
            // Event triggerelése az idővel
            FilterProcessed?.Invoke(this, new FilterProcessedEventArgs 
            { 
                FilterName = "Invert", 
                ProcessingTimeMs = processingTime 
            });

            LayerChanged?.Invoke(this, EventArgs.Empty);
        }

        public void ApplyFlip(FlipDirection direction)
        {
            if (CurrentProject?.ActiveLayer?.Content == null) return;

            // Stopper indítása
            var stopwatch = Stopwatch.StartNew();

            var processor = new FlipProcessor(direction);
            var command = new FilterCommand(CurrentProject.ActiveLayer, processor, $"Flip {direction}");

            _commandHistory.Execute(command);

            // Stopper leállítása és idő mérése
            stopwatch.Stop();
            var processingTime = stopwatch.ElapsedMilliseconds;
            
            // Event triggerelése az idővel
            FilterProcessed?.Invoke(this, new FilterProcessedEventArgs 
            { 
                FilterName = "Invert", 
                ProcessingTimeMs = processingTime 
            });

            LayerChanged?.Invoke(this, EventArgs.Empty);
        }

        public void ApplyGamma(double gamma)
        {
            if (CurrentProject?.ActiveLayer?.Content == null) return;

            var stopwatch = Stopwatch.StartNew();

            var processor = new GammaProcessor(_lutService, gamma);
            var command = new FilterCommand(CurrentProject.ActiveLayer, processor, $"Gamma {gamma}");

            _commandHistory.Execute(command);

            stopwatch.Stop();
            FilterProcessed?.Invoke(this, new FilterProcessedEventArgs
            {
                FilterName = $"Gamma {gamma:0.0}",
                ProcessingTimeMs = stopwatch.ElapsedMilliseconds
            });

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

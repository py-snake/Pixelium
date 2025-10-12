using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Pixelium.Core.Models;
using Pixelium.Core.Processors;
using Pixelium.Core.Services;
using SkiaSharp;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Pixelium.UI.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private readonly ImageService _imageService;
        private Bitmap? _imageSource;
        private Window? _mainWindow;
        private string _statusMessage = "Ready";

        public Bitmap? ImageSource
        {
            get => _imageSource;
            set
            {
                _imageSource = value;
                OnPropertyChanged();
            }
        }

        public ICommand NewCommand { get; }
        public ICommand OpenCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand ExitCommand { get; }
        public ICommand UndoCommand { get; }
        public ICommand RedoCommand { get; }
        public ICommand GrayscaleCommand { get; }
        public ICommand InvertCommand { get; }
        public ICommand FlipHorizontalCommand { get; }
        public ICommand FlipVerticalCommand { get; }

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }

            private double _gammaValue = 1.0;
        private bool _isGammaDialogOpen;

        public double GammaValue
        {
            get => _gammaValue;
            set
            {
                _gammaValue = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(GammaDisplayValue));
            }
        }

        public string GammaDisplayValue => $"Gamma: {_gammaValue:0.0}";

        public bool IsGammaDialogOpen
        {
            get => _isGammaDialogOpen;
            set
            {
                _isGammaDialogOpen = value;
                OnPropertyChanged();
            }
        }

        public ICommand OpenGammaDialogCommand { get; }
        public ICommand ApplyGammaCommand { get; }
        public ICommand CancelGammaCommand { get; }

        public MainWindowViewModel()
        {
            _imageService = new ImageService();
            _imageService.LayerChanged += OnLayerChanged;
            _imageService.FilterProcessed += OnFilterProcessed;

            // Simple command implementation
            NewCommand = new SimpleCommand(CreateNew);
            OpenCommand = new SimpleCommand(async () => await OpenImage());
            SaveCommand = new SimpleCommand(async () => await SaveImage());
            ExitCommand = new SimpleCommand(() => Environment.Exit(0));
            UndoCommand = new SimpleCommand(_imageService.Undo);
            RedoCommand = new SimpleCommand(_imageService.Redo);
            GrayscaleCommand = new SimpleCommand(_imageService.ApplyGrayscale);
            InvertCommand = new SimpleCommand(_imageService.ApplyInvert);
            FlipHorizontalCommand = new SimpleCommand(() => _imageService.ApplyFlip(FlipDirection.Horizontal));
            FlipVerticalCommand = new SimpleCommand(() => _imageService.ApplyFlip(FlipDirection.Vertical));
            OpenGammaDialogCommand = new SimpleCommand(() => 
            {
                GammaValue = 1.0; // Reset to default
                IsGammaDialogOpen = true;
            });

            ApplyGammaCommand = new SimpleCommand(() =>
            {
                _imageService.ApplyGamma(GammaValue);
                IsGammaDialogOpen = false;
            });

            CancelGammaCommand = new SimpleCommand(() => IsGammaDialogOpen = false);
                // Initialize with test project
                CreateNew();
                CreateTestImage();
            }

        // Call this method from your MainWindow after it's loaded
        public void SetMainWindow(Window window)
        {
            _mainWindow = window;
        }

        private void CreateNew()
        {
            _imageService.CreateNewProject(800, 600);
        }

        private async Task OpenImage()
        {
            if (_mainWindow == null) return;

            var storageProvider = _mainWindow.StorageProvider;

            var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Open Image",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Image Files")
                    {
                        Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.bmp", "*.gif" },
                        MimeTypes = new[] { "image/*" }
                    },
                    new FilePickerFileType("All Files")
                    {
                        Patterns = new[] { "*.*" }
                    }
                }
            });

            if (files.Count > 0)
            {
                var file = files[0];
                try
                {
                    await using var stream = await file.OpenReadAsync();
                    using var memoryStream = new MemoryStream();
                    await stream.CopyToAsync(memoryStream);
                    memoryStream.Position = 0;

                    // Load the image using SkiaSharp
                    using var skBitmap = SKBitmap.Decode(memoryStream);
                    if (skBitmap != null)
                    {
                        // Create new project with the loaded image dimensions
                        _imageService.CreateNewProject(skBitmap.Width, skBitmap.Height);

                        // Copy the loaded image to the active layer
                        if (_imageService.CurrentProject?.ActiveLayer?.Content != null)
                        {
                            var canvas = new SKCanvas(_imageService.CurrentProject.ActiveLayer.Content);
                            canvas.DrawBitmap(skBitmap, 0, 0);
                            canvas.Dispose();

                            Dispatcher.UIThread.Post(() => OnLayerChanged(this, EventArgs.Empty));
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error opening image: {ex.Message}");
                }
            }
        }

        private async Task SaveImage()
        {
            if (_mainWindow == null) return;

            var storageProvider = _mainWindow.StorageProvider;

            var file = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save Image",
                DefaultExtension = "png",
                SuggestedFileName = "pixelium_output.png",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("PNG Image")
                    {
                        Patterns = new[] { "*.png" },
                        MimeTypes = new[] { "image/png" }
                    },
                    new FilePickerFileType("JPEG Image")
                    {
                        Patterns = new[] { "*.jpg", "*.jpeg" },
                        MimeTypes = new[] { "image/jpeg" }
                    },
                    new FilePickerFileType("All Files")
                    {
                        Patterns = new[] { "*.*" }
                    }
                }
            });

            if (file != null)
            {
                try
                {
                    var filePath = file.Path.LocalPath;
                    if (_imageService.SaveImage(filePath))
                    {
                        System.Diagnostics.Debug.WriteLine($"Image saved to: {filePath}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error saving image: {ex.Message}");
                }
            }
        }

        private void CreateTestImage()
        {
            if (_imageService.CurrentProject?.ActiveLayer?.Content != null)
            {
                var bitmap = _imageService.CurrentProject.ActiveLayer.Content;
                using var canvas = new SKCanvas(bitmap);

                canvas.Clear(SKColors.White);

                using var paint = new SKPaint
                {
                    IsAntialias = true,
                    TextSize = 48,
                    Color = SKColors.Blue
                };

                canvas.DrawText("Pixelium Test Image", 50, 100, paint);

                using var gradientPaint = new SKPaint
                {
                    Shader = SKShader.CreateLinearGradient(
                        new SKPoint(0, 200),
                        new SKPoint(400, 400),
                        new[] { SKColors.Red, SKColors.Green, SKColors.Blue },
                        null,
                        SKShaderTileMode.Clamp)
                };

                canvas.DrawRect(new SKRect(50, 200, 450, 400), gradientPaint);

                Dispatcher.UIThread.Post(() => OnLayerChanged(this, EventArgs.Empty));
            }
        }

        private void OnLayerChanged(object? sender, EventArgs e)
        {
            if (_imageService.CurrentProject?.ActiveLayer?.Content != null)
            {
                Dispatcher.UIThread.Post(() =>
                    ImageSource = SKBitmapToAvaloniaBitmap(_imageService.CurrentProject.ActiveLayer.Content));
            }
        }

        private void OnFilterProcessed(object? sender, FilterProcessedEventArgs e)
        {
            Dispatcher.UIThread.Post(() =>
            {
                StatusMessage = $"{e.FilterName} applied in {e.ProcessingTimeMs} ms";
            });
        }

        private static Bitmap SKBitmapToAvaloniaBitmap(SKBitmap skBitmap)
        {
            using var image = SKImage.FromBitmap(skBitmap);
            using var data = image.Encode();
            using var stream = new MemoryStream(data.ToArray());
            return new Bitmap(stream);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // Simple command implementation
    public class SimpleCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public SimpleCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter)
        {
            return _canExecute?.Invoke() ?? true;
        }

        public void Execute(object? parameter)
        {
            _execute();
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}

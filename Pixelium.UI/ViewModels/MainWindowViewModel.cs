using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Pixelium.Core.Models;
using Pixelium.Core.Processors;
using Pixelium.Core.Services;
using SkiaSharp;
using System;
using System.Collections.ObjectModel;
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
        private Layer? _selectedLayer;
        private double _zoomLevel = 1.0;
        private Stretch _imageStretch = Stretch.None;
        private double _imageDisplayWidth = double.NaN;
        private double _imageDisplayHeight = double.NaN;
        private bool _isNonDestructiveMode = true;
        private double _viewportWidth = 800;
        private double _viewportHeight = 600;

        public Bitmap? ImageSource
        {
            get => _imageSource;
            set
            {
                _imageSource = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<Layer> Layers => _imageService.CurrentProject?.Layers ?? new ObservableCollection<Layer>();

        public Layer? SelectedLayer
        {
            get => _selectedLayer;
            set
            {
                _selectedLayer = value;
                if (value != null)
                {
                    _imageService.SetActiveLayer(value);
                }
                OnPropertyChanged();
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }

        public double ZoomLevel
        {
            get => _zoomLevel;
            set
            {
                _zoomLevel = value;
                UpdateImageDisplay();
                OnPropertyChanged();
                OnPropertyChanged(nameof(ZoomPercentage));
            }
        }

        public double ZoomPercentage => _zoomLevel * 100;

        public Stretch ImageStretch
        {
            get => _imageStretch;
            set
            {
                _imageStretch = value;
                OnPropertyChanged();
            }
        }

        public double ImageDisplayWidth
        {
            get => _imageDisplayWidth;
            set
            {
                _imageDisplayWidth = value;
                OnPropertyChanged();
            }
        }

        public double ImageDisplayHeight
        {
            get => _imageDisplayHeight;
            set
            {
                _imageDisplayHeight = value;
                OnPropertyChanged();
            }
        }

        public bool IsNonDestructiveMode
        {
            get => _isNonDestructiveMode;
            set
            {
                _isNonDestructiveMode = value;
                _imageService.CurrentEditMode = value ? EditMode.NonDestructive : EditMode.Destructive;
                OnPropertyChanged();
                OnPropertyChanged(nameof(EditModeDescription));

                // Update undo/redo button states (only work in destructive mode)
                ((SimpleCommand)UndoCommand).RaiseCanExecuteChanged();
                ((SimpleCommand)RedoCommand).RaiseCanExecuteChanged();
            }
        }

        public string EditModeDescription => _isNonDestructiveMode
            ? "Non-Destructive: Each edit creates a new layer"
            : "Destructive: Edits modify active layer (uses undo/redo)";

        // File Commands
        public ICommand NewCommand { get; }
        public ICommand OpenCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand ExitCommand { get; }
        public ICommand CreateSample1Command { get; }
        public ICommand CreateSample2Command { get; }
        public ICommand CreateSample3Command { get; }

        // Edit Commands
        public ICommand UndoCommand { get; }
        public ICommand RedoCommand { get; }

        // View Commands (ZOOM)
        public ICommand ZoomInCommand { get; }
        public ICommand ZoomOutCommand { get; }
        public ICommand ZoomActualCommand { get; }
        public ICommand FitToWidthCommand { get; }
        public ICommand FitToHeightCommand { get; }
        public ICommand FitToScreenCommand { get; }
        public ICommand StretchCommand { get; }

        // Basic Transformation Commands
        public ICommand GrayscaleCommand { get; }
        public ICommand InvertCommand { get; }
        public ICommand FlipHorizontalCommand { get; }
        public ICommand FlipVerticalCommand { get; }

        // Advanced Filter Commands
        public ICommand OpenGammaDialogCommand { get; }
        public ICommand ApplyGammaCommand { get; }
        public ICommand CancelGammaCommand { get; }
        public ICommand OpenLogarithmicDialogCommand { get; }
        public ICommand ApplyLogarithmicCommand { get; }
        public ICommand CancelLogarithmicCommand { get; }

        // Histogram Commands
        public ICommand ShowHistogramCommand { get; }
        public ICommand ApplyHistogramEqualizationCommand { get; }

        // Filter Commands
        public ICommand OpenBoxFilterDialogCommand { get; }
        public ICommand ApplyBoxFilterCommand { get; }
        public ICommand CancelBoxFilterCommand { get; }
        public ICommand OpenGaussianDialogCommand { get; }
        public ICommand ApplyGaussianFilterCommand { get; }
        public ICommand CancelGaussianCommand { get; }
        public ICommand ApplySobelCommand { get; }
        public ICommand ApplyLaplaceCommand { get; }
        public ICommand OpenHarrisDialogCommand { get; }

        // Layer Commands
        public ICommand AddLayerCommand { get; }
        public ICommand RemoveLayerCommand { get; }
        public ICommand DuplicateLayerCommand { get; }
        public ICommand MergeDownCommand { get; }

        // Dialog Properties
        private double _gammaValue = 1.0;
        private bool _isGammaDialogOpen;
        private double _logarithmicC = 1.0;
        private bool _isLogarithmicDialogOpen;
        private int _boxFilterSize = 3;
        private bool _isBoxFilterDialogOpen;
        private float _gaussianSigma = 1.0f;
        private int _gaussianSize = 5;
        private bool _isGaussianDialogOpen;

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

        public string GammaDisplayValue => $"Gamma: {_gammaValue:0.00}";

        public bool IsGammaDialogOpen
        {
            get => _isGammaDialogOpen;
            set
            {
                _isGammaDialogOpen = value;
                OnPropertyChanged();
            }
        }

        public double LogarithmicC
        {
            get => _logarithmicC;
            set
            {
                _logarithmicC = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(LogarithmicDisplayValue));
            }
        }

        public string LogarithmicDisplayValue => $"C: {_logarithmicC:0.00}";

        public bool IsLogarithmicDialogOpen
        {
            get => _isLogarithmicDialogOpen;
            set
            {
                _isLogarithmicDialogOpen = value;
                OnPropertyChanged();
            }
        }

        public int BoxFilterSize
        {
            get => _boxFilterSize;
            set
            {
                _boxFilterSize = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(BoxFilterDisplayValue));
            }
        }

        public string BoxFilterDisplayValue => $"Size: {_boxFilterSize}x{_boxFilterSize}";

        public bool IsBoxFilterDialogOpen
        {
            get => _isBoxFilterDialogOpen;
            set
            {
                _isBoxFilterDialogOpen = value;
                OnPropertyChanged();
            }
        }

        public float GaussianSigma
        {
            get => _gaussianSigma;
            set
            {
                _gaussianSigma = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(GaussianDisplayValue));
            }
        }

        public int GaussianSize
        {
            get => _gaussianSize;
            set
            {
                _gaussianSize = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(GaussianDisplayValue));
            }
        }

        public string GaussianDisplayValue => $"σ: {_gaussianSigma:0.0}, Size: {_gaussianSize}x{_gaussianSize}";

        public bool IsGaussianDialogOpen
        {
            get => _isGaussianDialogOpen;
            set
            {
                _isGaussianDialogOpen = value;
                OnPropertyChanged();
            }
        }

        public MainWindowViewModel()
        {
            _imageService = new ImageService();
            _imageService.LayerChanged += OnLayerChanged;
            _imageService.FilterProcessed += OnFilterProcessed;
            _imageService.ProjectChanged += OnProjectChanged;

            // File Commands
            NewCommand = new SimpleCommand(CreateNew);
            OpenCommand = new SimpleCommand(async () => await OpenImage());
            SaveCommand = new SimpleCommand(async () => await SaveImage());
            ExitCommand = new SimpleCommand(() => Environment.Exit(0));
            CreateSample1Command = new SimpleCommand(CreateSample1);
            CreateSample2Command = new SimpleCommand(CreateSample2);
            CreateSample3Command = new SimpleCommand(CreateSample3);

            // Edit Commands
            UndoCommand = new SimpleCommand(_imageService.Undo, () => _imageService.CanUndo);
            RedoCommand = new SimpleCommand(_imageService.Redo, () => _imageService.CanRedo);

            // View/Zoom Commands
            ZoomInCommand = new SimpleCommand(() => ZoomLevel = Math.Min(ZoomLevel * 1.25, 10.0));
            ZoomOutCommand = new SimpleCommand(() => ZoomLevel = Math.Max(ZoomLevel / 1.25, 0.1));
            ZoomActualCommand = new SimpleCommand(() => { ImageStretch = Stretch.None; ZoomLevel = 1.0; });
            FitToWidthCommand = new SimpleCommand(FitToWidth);
            FitToHeightCommand = new SimpleCommand(FitToHeight);
            FitToScreenCommand = new SimpleCommand(FitToScreen);
            StretchCommand = new SimpleCommand(() => { ImageStretch = Stretch.Fill; UpdateImageDisplay(); });

            // Basic Transformations
            GrayscaleCommand = new SimpleCommand(_imageService.ApplyGrayscale);
            InvertCommand = new SimpleCommand(_imageService.ApplyInvert);
            FlipHorizontalCommand = new SimpleCommand(() => _imageService.ApplyFlip(FlipDirection.Horizontal));
            FlipVerticalCommand = new SimpleCommand(() => _imageService.ApplyFlip(FlipDirection.Vertical));

            // Advanced Filters with Dialogs
            OpenGammaDialogCommand = new SimpleCommand(() => 
            {
                GammaValue = 1.0;
                IsGammaDialogOpen = true;
            });
            ApplyGammaCommand = new SimpleCommand(() =>
            {
                _imageService.ApplyGamma(GammaValue);
                IsGammaDialogOpen = false;
            });
            CancelGammaCommand = new SimpleCommand(() => IsGammaDialogOpen = false);

            OpenLogarithmicDialogCommand = new SimpleCommand(() =>
            {
                LogarithmicC = 1.0;
                IsLogarithmicDialogOpen = true;
            });
            ApplyLogarithmicCommand = new SimpleCommand(() =>
            {
                _imageService.ApplyLogarithmic(LogarithmicC);
                IsLogarithmicDialogOpen = false;
            });
            CancelLogarithmicCommand = new SimpleCommand(() => IsLogarithmicDialogOpen = false);

            // Histogram
            ShowHistogramCommand = new SimpleCommand(ShowHistogram);
            ApplyHistogramEqualizationCommand = new SimpleCommand(_imageService.ApplyHistogramEqualization);

            // Filters with Dialogs
            OpenBoxFilterDialogCommand = new SimpleCommand(() =>
            {
                BoxFilterSize = 3;
                IsBoxFilterDialogOpen = true;
            });
            ApplyBoxFilterCommand = new SimpleCommand(() =>
            {
                _imageService.ApplyBoxFilter(BoxFilterSize);
                IsBoxFilterDialogOpen = false;
            });
            CancelBoxFilterCommand = new SimpleCommand(() => IsBoxFilterDialogOpen = false);
            
            OpenGaussianDialogCommand = new SimpleCommand(() =>
            {
                GaussianSigma = 1.0f;
                GaussianSize = 5;
                IsGaussianDialogOpen = true;
            });
            ApplyGaussianFilterCommand = new SimpleCommand(() =>
            {
                _imageService.ApplyGaussianFilter(GaussianSigma, GaussianSize);
                IsGaussianDialogOpen = false;
            });
            CancelGaussianCommand = new SimpleCommand(() => IsGaussianDialogOpen = false);

            ApplySobelCommand = new SimpleCommand(_imageService.ApplySobelEdgeDetection);
            ApplyLaplaceCommand = new SimpleCommand(_imageService.ApplyLaplaceEdgeDetection);

            OpenHarrisDialogCommand = new SimpleCommand(OpenHarrisDialog);

            // Layer Commands
            AddLayerCommand = new SimpleCommand(() => _imageService.AddNewLayer());
            RemoveLayerCommand = new SimpleCommand(() => _imageService.RemoveActiveLayer());
            DuplicateLayerCommand = new SimpleCommand(() => _imageService.DuplicateActiveLayer());
            MergeDownCommand = new SimpleCommand(() => _imageService.MergeLayerDown());

            // Initialize with test project
            CreateNew();
            CreateSample1();
        }

        public void SetMainWindow(Window window)
        {
            _mainWindow = window;

            // Set initial size from window
            UpdateViewportSize();
        }

        private void UpdateViewportSize()
        {
            if (_mainWindow != null)
            {
                _viewportWidth = _mainWindow.ClientSize.Width - 250; // Subtract layer panel width
                _viewportHeight = _mainWindow.ClientSize.Height - 100; // Subtract menu and status bar
            }
        }

        private void FitToWidth()
        {
            if (_imageService.CurrentProject?.ActiveLayer?.Content == null) return;

            UpdateViewportSize();
            var bitmap = _imageService.CurrentProject.ActiveLayer.Content;
            ImageStretch = Stretch.None;
            ZoomLevel = _viewportWidth / bitmap.Width;
        }

        private void FitToHeight()
        {
            if (_imageService.CurrentProject?.ActiveLayer?.Content == null) return;

            UpdateViewportSize();
            var bitmap = _imageService.CurrentProject.ActiveLayer.Content;
            ImageStretch = Stretch.None;
            ZoomLevel = _viewportHeight / bitmap.Height;
        }

        private void FitToScreen()
        {
            if (_imageService.CurrentProject?.ActiveLayer?.Content == null) return;

            UpdateViewportSize();
            var bitmap = _imageService.CurrentProject.ActiveLayer.Content;
            ImageStretch = Stretch.None;

            // Calculate zoom to fit both width and height
            double widthZoom = _viewportWidth / bitmap.Width;
            double heightZoom = _viewportHeight / bitmap.Height;

            // Use the smaller zoom to ensure entire image fits
            ZoomLevel = Math.Min(widthZoom, heightZoom);
        }

        private void CreateNew()
        {
            _imageService.CreateNewProject(800, 600);
            OnProjectChanged(this, EventArgs.Empty);
        }

        private void UpdateImageDisplay()
        {
            if (_imageService.CurrentProject?.ActiveLayer?.Content != null && ImageStretch == Stretch.None)
            {
                var bitmap = _imageService.CurrentProject.ActiveLayer.Content;
                ImageDisplayWidth = bitmap.Width * _zoomLevel;
                ImageDisplayHeight = bitmap.Height * _zoomLevel;
            }
            else
            {
                ImageDisplayWidth = double.NaN;
                ImageDisplayHeight = double.NaN;
            }
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
                    var filePath = file.Path.LocalPath;
                    if (_imageService.LoadImage(filePath))
                    {
                        StatusMessage = $"Loaded: {Path.GetFileName(filePath)}";
                        OnProjectChanged(this, EventArgs.Empty);

                        // Auto-fit image to screen
                        FitToScreen();
                    }
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Error opening image: {ex.Message}";
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
                    new FilePickerFileType("BMP Image")
                    {
                        Patterns = new[] { "*.bmp" },
                        MimeTypes = new[] { "image/bmp" }
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
                        StatusMessage = $"Saved: {Path.GetFileName(filePath)}";
                    }
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Error saving image: {ex.Message}";
                }
            }
        }

        private void ShowHistogram()
        {
            if (_imageService.CurrentProject?.ActiveLayer == null)
            {
                StatusMessage = "No image loaded";
                return;
            }

            var histogram = _imageService.GetHistogram();
            StatusMessage = $"Histogram calculated - Total pixels: {histogram.TotalPixels:N0}";

            // Create and show simple histogram window
            var histogramWindow = new Views.SimpleHistogramWindow();
            histogramWindow.DisplayHistogram(histogram);
            histogramWindow.Show();
        }

        private async void OpenHarrisDialog()
        {
            if (_imageService.CurrentProject?.ActiveLayer == null)
            {
                StatusMessage = "No image loaded";
                return;
            }

            var dialog = new Views.HarrisCornerDialog();

            if (_mainWindow != null)
            {
                await dialog.ShowDialog(_mainWindow);

                if (dialog.WasApplied)
                {
                    _imageService.ApplyHarrisCornerDetection(dialog.Threshold, dialog.K, dialog.Sigma);
                }
            }
        }

        private void CreateSample1()
        {
            CreateNew();
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

                canvas.DrawText("Pixelium Image Editor", 50, 100, paint);

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

                StatusMessage = "Sample 1: Gradient & Text - Good for general filter testing";
                Dispatcher.UIThread.Post(() => OnLayerChanged(this, EventArgs.Empty));
            }
        }

        private void CreateSample2()
        {
            CreateNew();
            if (_imageService.CurrentProject?.ActiveLayer?.Content != null)
            {
                var bitmap = _imageService.CurrentProject.ActiveLayer.Content;
                using var canvas = new SKCanvas(bitmap);

                canvas.Clear(new SKColor(240, 240, 240));

                // Draw checkerboard pattern (good for edge detection)
                using var blackPaint = new SKPaint { Color = SKColors.Black, IsAntialias = false };
                using var whitePaint = new SKPaint { Color = SKColors.White, IsAntialias = false };

                int squareSize = 60;
                for (int y = 0; y < bitmap.Height; y += squareSize)
                {
                    for (int x = 0; x < bitmap.Width; x += squareSize)
                    {
                        bool isBlack = ((x / squareSize) + (y / squareSize)) % 2 == 0;
                        canvas.DrawRect(x, y, squareSize, squareSize, isBlack ? blackPaint : whitePaint);
                    }
                }

                // Draw colored circles (good for Harris corner detection)
                using var redPaint = new SKPaint { Color = SKColors.Red, IsAntialias = true, Style = SKPaintStyle.Fill };
                using var greenPaint = new SKPaint { Color = SKColors.Green, IsAntialias = true, Style = SKPaintStyle.Fill };
                using var bluePaint = new SKPaint { Color = SKColors.Blue, IsAntialias = true, Style = SKPaintStyle.Fill };
                using var yellowPaint = new SKPaint { Color = SKColors.Yellow, IsAntialias = true, Style = SKPaintStyle.Fill };

                canvas.DrawCircle(200, 150, 80, redPaint);
                canvas.DrawCircle(600, 150, 80, greenPaint);
                canvas.DrawCircle(200, 450, 80, bluePaint);
                canvas.DrawCircle(600, 450, 80, yellowPaint);

                // Draw rectangles with different colors (good for various filters)
                using var magentaPaint = new SKPaint { Color = SKColors.Magenta, IsAntialias = true };
                using var cyanPaint = new SKPaint { Color = SKColors.Cyan, IsAntialias = true };

                canvas.DrawRect(350, 100, 100, 150, magentaPaint);
                canvas.DrawRect(350, 350, 100, 150, cyanPaint);

                StatusMessage = "Sample 2: Geometric Shapes - Good for edge detection & Harris corners";
                Dispatcher.UIThread.Post(() => OnLayerChanged(this, EventArgs.Empty));
            }
        }

        private void CreateSample3()
        {
            CreateNew();
            if (_imageService.CurrentProject?.ActiveLayer?.Content != null)
            {
                var bitmap = _imageService.CurrentProject.ActiveLayer.Content;
                using var canvas = new SKCanvas(bitmap);

                canvas.Clear(SKColors.White);

                // Create radial gradient background (good for histogram equalization)
                using var radialPaint = new SKPaint
                {
                    Shader = SKShader.CreateRadialGradient(
                        new SKPoint(400, 300),
                        300,
                        new[] { new SKColor(255, 200, 100), new SKColor(100, 150, 255), new SKColor(50, 50, 50) },
                        new float[] { 0, 0.5f, 1 },
                        SKShaderTileMode.Clamp)
                };
                canvas.DrawRect(0, 0, bitmap.Width, bitmap.Height, radialPaint);

                // Draw brightness levels (good for gamma/logarithmic testing)
                using var paint = new SKPaint { IsAntialias = true };
                for (int i = 0; i < 8; i++)
                {
                    int brightness = (i * 255) / 7;
                    paint.Color = new SKColor((byte)brightness, (byte)brightness, (byte)brightness);
                    canvas.DrawRect(50 + i * 90, 50, 80, 80, paint);
                }

                // Draw RGB color bars (good for grayscale testing)
                using var redPaint = new SKPaint { Color = new SKColor(255, 0, 0), IsAntialias = true };
                using var greenPaint = new SKPaint { Color = new SKColor(0, 255, 0), IsAntialias = true };
                using var bluePaint = new SKPaint { Color = new SKColor(0, 0, 255), IsAntialias = true };

                canvas.DrawRect(100, 450, 150, 100, redPaint);
                canvas.DrawRect(325, 450, 150, 100, greenPaint);
                canvas.DrawRect(550, 450, 150, 100, bluePaint);

                // Draw text with shadow (good for blur testing)
                using var shadowPaint = new SKPaint
                {
                    Color = new SKColor(0, 0, 0, 128),
                    IsAntialias = true,
                    TextSize = 36
                };
                using var textPaint = new SKPaint
                {
                    Color = SKColors.White,
                    IsAntialias = true,
                    TextSize = 36,
                    FakeBoldText = true
                };

                canvas.DrawText("COLOR TEST PATTERN", 205, 270, shadowPaint);
                canvas.DrawText("COLOR TEST PATTERN", 200, 265, textPaint);

                StatusMessage = "Sample 3: Color Test Pattern - Good for histogram, gamma & color filters";
                Dispatcher.UIThread.Post(() => OnLayerChanged(this, EventArgs.Empty));
            }
        }

        private void OnLayerChanged(object? sender, EventArgs e)
        {
            if (_imageService.CurrentProject != null)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    // Subscribe to any new layers that were added
                    foreach (var layer in _imageService.CurrentProject.Layers)
                    {
                        layer.PropertyChanged -= OnLayerPropertyChanged;
                        layer.PropertyChanged += OnLayerPropertyChanged;
                    }

                    // Flatten and display all layers
                    using var flattened = _imageService.CurrentProject.FlattenLayers();
                    ImageSource = SKBitmapToAvaloniaBitmap(flattened);
                    UpdateImageDisplay();

                    // Update the layer list binding
                    OnPropertyChanged(nameof(Layers));
                });
            }
        }

        private void OnProjectChanged(object? sender, EventArgs e)
        {
            Dispatcher.UIThread.Post(() =>
            {
                // Subscribe to project's Layers property changes
                if (_imageService.CurrentProject != null)
                {
                    _imageService.CurrentProject.PropertyChanged -= OnProjectPropertyChanged;
                    _imageService.CurrentProject.PropertyChanged += OnProjectPropertyChanged;

                    // Also subscribe directly to each layer's property changes
                    foreach (var layer in _imageService.CurrentProject.Layers)
                    {
                        layer.PropertyChanged -= OnLayerPropertyChanged;
                        layer.PropertyChanged += OnLayerPropertyChanged;
                    }
                }

                OnPropertyChanged(nameof(Layers));
                if (_imageService.CurrentProject?.ActiveLayer != null)
                {
                    SelectedLayer = _imageService.CurrentProject.ActiveLayer;
                }
                UpdateImageDisplay();

                // Update undo/redo button states (should be disabled for new/loaded projects)
                ((SimpleCommand)UndoCommand).RaiseCanExecuteChanged();
                ((SimpleCommand)RedoCommand).RaiseCanExecuteChanged();
            });
        }

        private void OnProjectPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Project.Layers))
            {
                // When any layer property changes, refresh the flattened image
                Dispatcher.UIThread.Post(() => OnLayerChanged(this, EventArgs.Empty));
            }
        }

        private void OnLayerPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // When any layer's Visible or Opacity changes, immediately refresh the image
            if (e.PropertyName == nameof(Layer.Visible) ||
                e.PropertyName == nameof(Layer.Opacity))
            {
                Dispatcher.UIThread.Post(() => OnLayerChanged(this, EventArgs.Empty));
            }
        }

        private void OnFilterProcessed(object? sender, FilterProcessedEventArgs e)
        {
            Dispatcher.UIThread.Post(() =>
            {
                StatusMessage = $"{e.FilterName} applied: {e.ProcessingTimeMs}ms (processing) / {e.TotalTimeMs}ms (total)";

                // Update undo/redo button states
                ((SimpleCommand)UndoCommand).RaiseCanExecuteChanged();
                ((SimpleCommand)RedoCommand).RaiseCanExecuteChanged();
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
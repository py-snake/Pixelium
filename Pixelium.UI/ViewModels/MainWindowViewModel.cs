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

        // File Commands
        public ICommand NewCommand { get; }
        public ICommand OpenCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand ExitCommand { get; }

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

            // Edit Commands
            UndoCommand = new SimpleCommand(_imageService.Undo);
            RedoCommand = new SimpleCommand(_imageService.Redo);

            // View/Zoom Commands
            ZoomInCommand = new SimpleCommand(() => ZoomLevel = Math.Min(ZoomLevel * 1.25, 10.0));
            ZoomOutCommand = new SimpleCommand(() => ZoomLevel = Math.Max(ZoomLevel / 1.25, 0.1));
            ZoomActualCommand = new SimpleCommand(() => { ImageStretch = Stretch.None; ZoomLevel = 1.0; });
            FitToWidthCommand = new SimpleCommand(() => ImageStretch = Stretch.UniformToFill);
            FitToHeightCommand = new SimpleCommand(() => ImageStretch = Stretch.Uniform);
            FitToScreenCommand = new SimpleCommand(() => ImageStretch = Stretch.Uniform);
            StretchCommand = new SimpleCommand(() => ImageStretch = Stretch.Fill);

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

            // Layer Commands
            AddLayerCommand = new SimpleCommand(() => _imageService.AddNewLayer());
            RemoveLayerCommand = new SimpleCommand(() => _imageService.RemoveActiveLayer());
            DuplicateLayerCommand = new SimpleCommand(() => _imageService.DuplicateActiveLayer());
            MergeDownCommand = new SimpleCommand(() => _imageService.MergeLayerDown());

            // Initialize with test project
            CreateNew();
            CreateTestImage();
        }

        public void SetMainWindow(Window window)
        {
            _mainWindow = window;
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
            var histogram = _imageService.GetHistogram();
            StatusMessage = $"Histogram calculated - Total pixels: {histogram.TotalPixels}";
            
            // Display histogram stats in debug output
            System.Diagnostics.Debug.WriteLine("=== Histogram Statistics ===");
            System.Diagnostics.Debug.WriteLine($"Total Pixels: {histogram.TotalPixels}");
            System.Diagnostics.Debug.WriteLine($"Red Channel - Min: {GetMinValue(histogram.Red)}, Max: {GetMaxValue(histogram.Red)}");
            System.Diagnostics.Debug.WriteLine($"Green Channel - Min: {GetMinValue(histogram.Green)}, Max: {GetMaxValue(histogram.Green)}");
            System.Diagnostics.Debug.WriteLine($"Blue Channel - Min: {GetMinValue(histogram.Blue)}, Max: {GetMaxValue(histogram.Blue)}");
        }

        private int GetMinValue(int[] histogram)
        {
            for (int i = 0; i < histogram.Length; i++)
                if (histogram[i] > 0) return i;
            return 0;
        }

        private int GetMaxValue(int[] histogram)
        {
            for (int i = histogram.Length - 1; i >= 0; i--)
                if (histogram[i] > 0) return i;
            return 255;
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

                Dispatcher.UIThread.Post(() => OnLayerChanged(this, EventArgs.Empty));
            }
        }

        private void OnLayerChanged(object? sender, EventArgs e)
        {
            if (_imageService.CurrentProject != null)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    // Flatten and display all layers
                    using var flattened = _imageService.CurrentProject.FlattenLayers();
                    ImageSource = SKBitmapToAvaloniaBitmap(flattened);
                    UpdateImageDisplay();
                });
            }
        }

        private void OnProjectChanged(object? sender, EventArgs e)
        {
            Dispatcher.UIThread.Post(() =>
            {
                OnPropertyChanged(nameof(Layers));
                if (_imageService.CurrentProject?.ActiveLayer != null)
                {
                    SelectedLayer = _imageService.CurrentProject.ActiveLayer;
                }
                UpdateImageDisplay();
            });
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
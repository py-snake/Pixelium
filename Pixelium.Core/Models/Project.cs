using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using SkiaSharp;

namespace Pixelium.Core.Models
{
    public class Project : INotifyPropertyChanged, IDisposable
    {
        private string _name = "Untitled";
        private Layer? _activeLayer;

        public string Name 
        { 
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged();
            }
        }

        public int Width { get; set; }
        public int Height { get; set; }
        public ObservableCollection<Layer> Layers { get; set; }
        
        public Layer? ActiveLayer 
        { 
            get => _activeLayer;
            set
            {
                _activeLayer = value;
                OnPropertyChanged();
            }
        }

        public Project(int width, int height)
        {
            Width = width;
            Height = height;
            Layers = new ObservableCollection<Layer>();

            var backgroundLayer = new Layer(width, height, "Background");
            using var canvas = new SKCanvas(backgroundLayer.Content);
            canvas.Clear(SKColors.White);
            
            Layers.Add(backgroundLayer);
            ActiveLayer = backgroundLayer;
        }

        public Layer AddLayer(string name = "New Layer")
        {
            var layer = new Layer(Width, Height, name);
            Layers.Add(layer);
            ActiveLayer = layer;
            OnPropertyChanged(nameof(Layers));
            return layer;
        }

        public void RemoveLayer(Layer layer)
        {
            if (Layers.Count <= 1) return;

            int index = Layers.IndexOf(layer);
            Layers.Remove(layer);
            layer.Dispose();

            if (ActiveLayer == layer)
            {
                ActiveLayer = index > 0 ? Layers[index - 1] : Layers[0];
            }

            OnPropertyChanged(nameof(Layers));
        }

        public void DuplicateLayer(Layer layer)
        {
            var duplicate = layer.Clone();
            int index = Layers.IndexOf(layer);
            Layers.Insert(index + 1, duplicate);
            ActiveLayer = duplicate;
            OnPropertyChanged(nameof(Layers));
        }

        public void MoveLayerUp(Layer layer)
        {
            int index = Layers.IndexOf(layer);
            if (index < Layers.Count - 1)
            {
                Layers.Move(index, index + 1);
                OnPropertyChanged(nameof(Layers));
            }
        }

        public void MoveLayerDown(Layer layer)
        {
            int index = Layers.IndexOf(layer);
            if (index > 0)
            {
                Layers.Move(index, index - 1);
                OnPropertyChanged(nameof(Layers));
            }
        }

        public SKBitmap FlattenLayers()
        {
            var flattened = new SKBitmap(Width, Height, SKColorType.Bgra8888, SKAlphaType.Premul);
            using var canvas = new SKCanvas(flattened);
            canvas.Clear(SKColors.Transparent);

            foreach (var layer in Layers)
            {
                if (!layer.Visible) continue;

                using var paint = new SKPaint
                {
                    BlendMode = layer.BlendMode,
                    Color = SKColors.White.WithAlpha((byte)(layer.Opacity * 255))
                };

                canvas.DrawBitmap(layer.Content, 0, 0, paint);
            }

            return flattened;
        }

        public void Dispose()
        {
            foreach (var layer in Layers)
            {
                layer.Dispose();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
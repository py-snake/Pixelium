using SkiaSharp;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Pixelium.Core.Models
{
    public class Layer : INotifyPropertyChanged, IDisposable
    {
        private string _name = "Layer";
        private float _opacity = 1.0f;
        private bool _visible = true;
        private bool _isLocked = false;
        private SKBitmap _content;

        public string Name 
        { 
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged();
            }
        }

        public SKBitmap Content
        {
            get => _content;
            set
            {
                if (_content != value)
                {
                    _content?.Dispose(); // Dispose old bitmap to prevent memory leak
                    _content = value;
                }
                OnPropertyChanged();
            }
        }

        public float Opacity 
        { 
            get => _opacity;
            set
            {
                _opacity = Math.Clamp(value, 0f, 1f);
                OnPropertyChanged();
            }
        }

        public bool Visible 
        { 
            get => _visible;
            set
            {
                _visible = value;
                OnPropertyChanged();
            }
        }

        public SKBlendMode BlendMode { get; set; } = SKBlendMode.SrcOver;
        
        public bool IsLocked 
        { 
            get => _isLocked;
            set
            {
                _isLocked = value;
                OnPropertyChanged();
            }
        }

        public Layer(int width, int height, string name = "Layer")
        {
            _content = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
            _name = name;
            
            using var canvas = new SKCanvas(_content);
            canvas.Clear(SKColors.Transparent);
        }

        public Layer Clone()
        {
            var clone = new Layer(_content.Width, _content.Height, $"{Name} copy")
            {
                Opacity = Opacity,
                Visible = Visible,
                BlendMode = BlendMode,
                IsLocked = IsLocked
            };

            _content.CopyTo(clone.Content);
            
            return clone;
        }

        public void Dispose()
        {
            _content?.Dispose();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
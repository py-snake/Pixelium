using Pixelium.Core.Models;
using Pixelium.Core.Processors;
using SkiaSharp;
using System;

namespace Pixelium.Core.Commands
{
    public class FilterCommand : IImageCommand, IDisposable
    {
        private readonly Layer _layer;
        private readonly IImageProcessor _processor;
        private SKBitmap? _backup; // Nullable field
        private bool _disposed = false;

        public string Name { get; }

        public FilterCommand(Layer layer, IImageProcessor processor, string name)
        {
            _layer = layer;
            _processor = processor;
            Name = name;
            _backup = null; // Explicit initialization
        }

        public bool Execute()
        {
            // Backup original
            _backup?.Dispose(); // Dispose any existing backup
            _backup = _layer.Content.Copy();

            // Apply filter
            return _processor.Process(_layer.Content);
        }

        public bool Undo()
        {
            if (_backup != null)
            {
                var oldContent = _layer.Content;
                _layer.Content = _backup;
                _backup = oldContent; // Keep old content as backup for redo
                return true;
            }
            return false;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources
                    _backup?.Dispose();
                    _backup = null;
                }
                _disposed = true;
            }
        }

        ~FilterCommand()
        {
            Dispose(false);
        }
    }
}

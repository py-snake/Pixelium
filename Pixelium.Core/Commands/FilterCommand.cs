using Pixelium.Core.Models;
using Pixelium.Core.Processors;
using SkiaSharp;

namespace Pixelium.Core.Commands
{
    public class FilterCommand : IImageCommand
    {
        private readonly Layer _layer;
        private readonly IImageProcessor _processor;
        private SKBitmap? _backup; // Nullable field

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
            _backup = _layer.Content.Copy();

            // Apply filter
            return _processor.Process(_layer.Content);
        }

        public bool Undo()
        {
            if (_backup != null)
            {
                _layer.Content.Dispose();
                _layer.Content = _backup;
                _backup = null;
                return true;
            }
            return false;
        }
    }
}

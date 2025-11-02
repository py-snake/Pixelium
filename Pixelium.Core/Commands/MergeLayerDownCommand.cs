using Pixelium.Core.Models;
using SkiaSharp;
using System;

namespace Pixelium.Core.Commands
{
    public class MergeLayerDownCommand : IImageCommand, IDisposable
    {
        private readonly Project _project;
        private Layer? _upperLayer;
        private Layer? _lowerLayer;
        private SKBitmap? _lowerLayerBackup;
        private int _upperLayerIndex;
        private Layer? _previousActiveLayer;
        private bool _disposed = false;

        public string Name => "Merge Layer Down";

        public MergeLayerDownCommand(Project project, Layer upperLayer, Layer lowerLayer)
        {
            _project = project;
            _upperLayer = upperLayer;
            _lowerLayer = lowerLayer;
        }

        public bool Execute()
        {
            if (_upperLayer == null || _lowerLayer == null) return false;

            // Save state for undo
            _upperLayerIndex = _project.Layers.IndexOf(_upperLayer);
            _previousActiveLayer = _project.ActiveLayer;

            // Backup lower layer content before merge
            _lowerLayerBackup?.Dispose();
            _lowerLayerBackup = _lowerLayer.Content.Copy();

            // Perform merge: composite upper onto lower
            using var canvas = new SKCanvas(_lowerLayer.Content);
            using var paint = new SKPaint
            {
                BlendMode = _upperLayer.BlendMode,
                Color = SKColors.White.WithAlpha((byte)(_upperLayer.Opacity * 255))
            };
            canvas.DrawBitmap(_upperLayer.Content, 0, 0, paint);

            // Remove upper layer (don't dispose - keep for undo)
            _project.Layers.Remove(_upperLayer);
            _project.ActiveLayer = _lowerLayer;

            return true;
        }

        public bool Undo()
        {
            if (_upperLayer == null || _lowerLayer == null || _lowerLayerBackup == null)
                return false;

            // Restore lower layer content
            var restored = _lowerLayerBackup.Copy();
            _lowerLayer.Content = restored;

            // Re-add upper layer at original position
            _project.Layers.Insert(_upperLayerIndex, _upperLayer);

            // Restore active layer
            if (_previousActiveLayer != null)
            {
                _project.ActiveLayer = _previousActiveLayer;
            }

            return true;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _lowerLayerBackup?.Dispose();
                _lowerLayerBackup = null;
                _disposed = true;
            }
        }
    }
}

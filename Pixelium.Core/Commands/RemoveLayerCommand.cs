using Pixelium.Core.Models;

namespace Pixelium.Core.Commands
{
    public class RemoveLayerCommand : IImageCommand
    {
        private readonly Project _project;
        private Layer? _removedLayer;
        private int _removedIndex;
        private Layer? _previousActiveLayer;

        public string Name => "Remove Layer";

        public RemoveLayerCommand(Project project, Layer layer)
        {
            _project = project;
            _removedLayer = layer;
        }

        public bool Execute()
        {
            if (_removedLayer == null || _project.Layers.Count <= 1)
                return false;

            _removedIndex = _project.Layers.IndexOf(_removedLayer);
            _previousActiveLayer = _project.ActiveLayer;

            _project.Layers.Remove(_removedLayer);

            // Set new active layer
            if (_project.ActiveLayer == _removedLayer)
            {
                _project.ActiveLayer = _removedIndex > 0
                    ? _project.Layers[_removedIndex - 1]
                    : _project.Layers[0];
            }

            return true;
        }

        public bool Undo()
        {
            if (_removedLayer == null) return false;

            _project.Layers.Insert(_removedIndex, _removedLayer);
            if (_previousActiveLayer != null)
            {
                _project.ActiveLayer = _previousActiveLayer;
            }

            return true;
        }
    }
}

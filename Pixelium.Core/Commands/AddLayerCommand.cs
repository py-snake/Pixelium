using Pixelium.Core.Models;

namespace Pixelium.Core.Commands
{
    public class AddLayerCommand : IImageCommand
    {
        private readonly Project _project;
        private Layer? _addedLayer;
        private readonly int _insertIndex;
        private Layer? _previousActiveLayer;

        public string Name { get; }

        public AddLayerCommand(Project project, Layer layer, int insertIndex, string name)
        {
            _project = project;
            _addedLayer = layer;
            _insertIndex = insertIndex;
            Name = name;
        }

        public bool Execute()
        {
            if (_addedLayer == null) return false;

            _previousActiveLayer = _project.ActiveLayer;
            _project.Layers.Insert(_insertIndex, _addedLayer);
            _project.ActiveLayer = _addedLayer;
            return true;
        }

        public bool Undo()
        {
            if (_addedLayer == null) return false;

            _project.Layers.Remove(_addedLayer);
            if (_previousActiveLayer != null)
            {
                _project.ActiveLayer = _previousActiveLayer;
            }
            return true;
        }
    }
}

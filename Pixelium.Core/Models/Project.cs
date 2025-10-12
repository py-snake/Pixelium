using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Pixelium.Core.Models
{
    public class Project : IDisposable
    {
        public string Name { get; set; } = "Untitled";
        public int Width { get; set; }
        public int Height { get; set; }
        public ObservableCollection<Layer> Layers { get; set; }
        public Layer ActiveLayer { get; set; }

        public Project(int width, int height)
        {
            Width = width;
            Height = height;
            Layers = new ObservableCollection<Layer>();

            // Create default layer
            var defaultLayer = new Layer(width, height) { Name = "Background" };
            Layers.Add(defaultLayer);
            ActiveLayer = defaultLayer;
        }

        public void Dispose()
        {
            foreach (var layer in Layers)
            {
                layer.Dispose();
            }
        }
    }
}

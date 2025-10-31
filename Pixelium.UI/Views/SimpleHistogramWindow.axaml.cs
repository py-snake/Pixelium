using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Media;
using Pixelium.Core.Processors;
using System;
using System.Linq;

namespace Pixelium.UI.Views
{
    public partial class SimpleHistogramWindow : Window
    {
        public SimpleHistogramWindow()
        {
            InitializeComponent();
        }

        public void DisplayHistogram(HistogramData histogram)
        {
            DrawHistogram(histogram.Luminosity, histogram.TotalPixels);
        }

        private void DrawHistogram(int[] data, int totalPixels)
        {
            HistogramCanvas.Children.Clear();

            double canvasWidth = HistogramCanvas.Width;
            double canvasHeight = HistogramCanvas.Height;
            double barWidth = canvasWidth / 256.0;

            // Find max value for scaling
            int maxValue = data.Max();
            if (maxValue == 0) maxValue = 1;

            // Calculate statistics
            int min = -1, max = -1;
            long sum = 0;
            for (int i = 0; i < 256; i++)
            {
                if (data[i] > 0)
                {
                    if (min == -1) min = i;
                    max = i;
                    sum += (long)i * data[i];
                }
            }
            double mean = totalPixels > 0 ? sum / (double)totalPixels : 0;

            // Update statistics text
            StatsText.Text = $"Total Pixels: {totalPixels:N0}  |  Min: {min}  |  Max: {max}  |  Mean: {mean:F2}";

            // Draw bars
            var barBrush = new SolidColorBrush(Color.FromRgb(100, 149, 237)); // Cornflower blue
            for (int i = 0; i < 256; i++)
            {
                if (data[i] == 0) continue;

                double barHeight = (data[i] / (double)maxValue) * canvasHeight;
                double x = i * barWidth;
                double y = canvasHeight - barHeight;

                var rect = new Rectangle
                {
                    Width = Math.Max(barWidth, 1),
                    Height = barHeight,
                    Fill = barBrush
                };

                Canvas.SetLeft(rect, x);
                Canvas.SetTop(rect, y);
                HistogramCanvas.Children.Add(rect);
            }

            // Draw grid lines
            var gridBrush = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255));
            for (int i = 0; i <= 4; i++)
            {
                double y = (canvasHeight / 4.0) * i;
                var line = new Line
                {
                    StartPoint = new Point(0, y),
                    EndPoint = new Point(canvasWidth, y),
                    Stroke = gridBrush,
                    StrokeThickness = 1
                };
                HistogramCanvas.Children.Add(line);
            }

            // Draw vertical lines for quartiles
            for (int i = 0; i <= 4; i++)
            {
                double x = (canvasWidth / 4.0) * i;
                var line = new Line
                {
                    StartPoint = new Point(x, 0),
                    EndPoint = new Point(x, canvasHeight),
                    Stroke = gridBrush,
                    StrokeThickness = 1
                };
                HistogramCanvas.Children.Add(line);
            }
        }

        private void CloseButton_Click(object? sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Pixelium.Core.Processors;
using System;
using System.Linq;

namespace Pixelium.UI.Views
{
    public partial class HistogramWindow : Window
    {
        public HistogramWindow()
        {
            InitializeComponent();
        }

        public void DisplayHistogram(HistogramData histogram)
        {
            // Update statistics
            TotalPixelsText.Text = $"Total Pixels: {histogram.TotalPixels:N0}";

            var redStats = CalculateStats(histogram.Red);
            var greenStats = CalculateStats(histogram.Green);
            var blueStats = CalculateStats(histogram.Blue);
            var lumStats = CalculateStats(histogram.Luminosity);

            RedStatsText.Text = $"Red - Min: {redStats.Min}, Max: {redStats.Max}, Mean: {redStats.Mean:F2}";
            GreenStatsText.Text = $"Green - Min: {greenStats.Min}, Max: {greenStats.Max}, Mean: {greenStats.Mean:F2}";
            BlueStatsText.Text = $"Blue - Min: {blueStats.Min}, Max: {blueStats.Max}, Mean: {blueStats.Mean:F2}";
            LuminosityStatsText.Text = $"Luminosity - Min: {lumStats.Min}, Max: {lumStats.Max}, Mean: {lumStats.Mean:F2}";

            // Draw histograms
            DrawHistogram(RedHistogramCanvas, histogram.Red, new SolidColorBrush(Color.FromRgb(255, 107, 107)));
            DrawHistogram(GreenHistogramCanvas, histogram.Green, new SolidColorBrush(Color.FromRgb(81, 207, 102)));
            DrawHistogram(BlueHistogramCanvas, histogram.Blue, new SolidColorBrush(Color.FromRgb(77, 171, 247)));
            DrawHistogram(LuminosityHistogramCanvas, histogram.Luminosity, new SolidColorBrush(Color.FromRgb(204, 204, 204)));
        }

        private void DrawHistogram(Canvas canvas, int[] data, IBrush color)
        {
            canvas.Children.Clear();

            if (data == null || data.Length == 0)
                return;

            double canvasWidth = canvas.Width;
            double canvasHeight = canvas.Height;
            double barWidth = canvasWidth / 256.0;

            // Find max value for scaling
            int maxValue = data.Max();
            if (maxValue == 0) maxValue = 1; // Prevent division by zero

            // Draw bars
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
                    Fill = color
                };

                Canvas.SetLeft(rect, x);
                Canvas.SetTop(rect, y);
                canvas.Children.Add(rect);
            }

            // Draw grid lines
            var gridBrush = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255));
            for (int i = 0; i < 5; i++)
            {
                double y = (canvasHeight / 4.0) * i;
                var line = new Line
                {
                    StartPoint = new Point(0, y),
                    EndPoint = new Point(canvasWidth, y),
                    Stroke = gridBrush,
                    StrokeThickness = 1
                };
                canvas.Children.Add(line);
            }
        }

        private (int Min, int Max, double Mean) CalculateStats(int[] histogram)
        {
            int min = -1, max = -1;
            long sum = 0;
            long count = 0;

            for (int i = 0; i < histogram.Length; i++)
            {
                if (histogram[i] > 0)
                {
                    if (min == -1) min = i;
                    max = i;
                    sum += (long)i * histogram[i];
                    count += histogram[i];
                }
            }

            double mean = count > 0 ? sum / (double)count : 0;
            return (min == -1 ? 0 : min, max == -1 ? 0 : max, mean);
        }
    }
}

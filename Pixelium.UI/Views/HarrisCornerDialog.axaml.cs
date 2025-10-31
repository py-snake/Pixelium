using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace Pixelium.UI.Views
{
    public partial class HarrisCornerDialog : Window
    {
        public float Threshold { get; private set; }
        public float K { get; private set; }
        public float Sigma { get; private set; }
        public bool WasApplied { get; private set; }

        public HarrisCornerDialog()
        {
            InitializeComponent();

            // Set default values
            Threshold = 0.01f;
            K = 0.04f;
            Sigma = 1.0f;
            WasApplied = false;

            // Wire up button click handlers
            var applyButton = this.FindControl<Button>("ApplyButton");
            var cancelButton = this.FindControl<Button>("CancelButton");

            if (applyButton != null)
            {
                applyButton.Click += OnApplyClick;
            }

            if (cancelButton != null)
            {
                cancelButton.Click += OnCancelClick;
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void OnApplyClick(object? sender, RoutedEventArgs e)
        {
            var thresholdSlider = this.FindControl<Slider>("ThresholdSlider");
            var kSlider = this.FindControl<Slider>("KSlider");
            var sigmaSlider = this.FindControl<Slider>("SigmaSlider");

            if (thresholdSlider != null)
                Threshold = (float)thresholdSlider.Value;
            if (kSlider != null)
                K = (float)kSlider.Value;
            if (sigmaSlider != null)
                Sigma = (float)sigmaSlider.Value;

            WasApplied = true;
            Close();
        }

        private void OnCancelClick(object? sender, RoutedEventArgs e)
        {
            WasApplied = false;
            Close();
        }
    }
}

using Avalonia.Controls;
using Pixelium.UI.ViewModels;

namespace Pixelium.UI.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            var viewModel = new MainWindowViewModel();
            DataContext = viewModel;
            viewModel.SetMainWindow(this);
        }
    }
}
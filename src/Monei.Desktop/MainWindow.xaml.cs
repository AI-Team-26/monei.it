using System.Windows;
using CommunityToolkit.Mvvm.DependencyInjection;

namespace Monei.Desktop;

public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
using CommunityToolkit.Mvvm.ComponentModel;

namespace Monei.Desktop;

public sealed partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _statusText = "Ready";

    public MainWindowViewModel()
    {
    }
}
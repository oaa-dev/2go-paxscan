using System.Windows;
using System.Windows.Controls;
using PaxScan.Desktop.ViewModels;

namespace PaxScan.Desktop.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        Loaded += async (_, _) => await viewModel.LoadAsync();
        Closed += (_, _) => viewModel.Devices.Dispose();
    }

    // PasswordBox.Password is not bindable by design; hand the value to the view model here.
    private void HubTokenBox_PasswordChanged(object sender, RoutedEventArgs e) =>
        _viewModel.Settings.NewHubToken = ((PasswordBox)sender).Password;
}

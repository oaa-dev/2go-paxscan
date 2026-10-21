using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using PaxScan.Desktop.ViewModels;
using PaxScan.Desktop.Views;

namespace PaxScan.Desktop.Services;

public interface IDialogService
{
    bool Confirm(string title, string message);

    void ShowError(string title, string message);

    void ShowPairDevice();
}

internal sealed class DialogService(IServiceProvider services) : IDialogService
{
    public bool Confirm(string title, string message) =>
        MessageBox.Show(Owner, message, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

    public void ShowError(string title, string message) =>
        MessageBox.Show(Owner, message, title, MessageBoxButton.OK, MessageBoxImage.Warning);

    public void ShowPairDevice()
    {
        var viewModel = services.GetRequiredService<PairDeviceViewModel>();
        var dialog = new PairDeviceDialog { DataContext = viewModel, Owner = Owner };
        viewModel.CloseRequested += (_, _) => dialog.Close();
        dialog.Closed += (_, _) => viewModel.Dispose();
        _ = viewModel.StartAsync();
        dialog.ShowDialog();
    }

    private static Window Owner => System.Windows.Application.Current.MainWindow;
}

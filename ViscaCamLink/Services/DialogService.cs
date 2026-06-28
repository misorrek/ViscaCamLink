namespace ViscaCamLink.Services;

using System.Reflection;

using ViscaCamLink.Updater;
using ViscaCamLink.Util;
using ViscaCamLink.ViewModels;
using ViscaCamLink.Views;

public sealed class DialogService(
    IOptionsViewModelFactory optionsViewModelFactory,
    IUpdateViewModelFactory updateViewModelFactory,
    IUpdateDownloadViewModelFactory updateDownloadViewModelFactory) : IDialogService
{
    public void ShowOptionsDialog()
    {
        var view = new OptionsView();
        var viewModel = optionsViewModelFactory.Create(() => view.Close());
        view.DataContext = viewModel;
        view.ShowDialog();
    }

    public void ShowUpdateDialog(UpdateInfo updateInfo)
    {
        var installedVersion = Assembly.GetEntryAssembly()?.GetName().Version;
        var view = new UpdateView();
        var viewModel = updateViewModelFactory.Create(updateInfo, installedVersion, () => view.Close(), ShowUpdateDownloadDialog);
        view.DataContext = viewModel;
        view.ShowDialog();
    }

    public void ShowUpdateDownloadDialog(UpdateInfo updateInfo)
    {
        var assetUrl = BuildInfo.IsPortable && updateInfo.PortableAssetUrl is not null
            ? updateInfo.PortableAssetUrl
            : updateInfo.InstallerAssetUrl;

        var view = new UpdateDownloadView();
        var viewModel = updateDownloadViewModelFactory.Create(assetUrl, () => view.Close());
        view.DataContext = viewModel;
        view.ShowDialog();
    }
}

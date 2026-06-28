namespace ViscaCamLink.ViewModels;

using ViscaCamLink.Services;
using ViscaCamLink.Updater;

public interface IOptionsViewModelFactory
{
    OptionsViewModel Create(Action closeHandler);
}

public sealed class OptionsViewModelFactory(
    ISettingsService settings,
    IHotKeyService hotKeyService) : IOptionsViewModelFactory
{
    public OptionsViewModel Create(Action closeHandler) =>
        new(settings, hotKeyService, closeHandler);
}

public interface IUpdateViewModelFactory
{
    UpdateViewModel Create(
        UpdateInfo updateInfo,
        Version? installedVersion,
        Action closeHandler,
        Action<UpdateInfo> showUpdateDownloadDialog);
}

public sealed class UpdateViewModelFactory : IUpdateViewModelFactory
{
    public UpdateViewModel Create(
        UpdateInfo updateInfo,
        Version? installedVersion,
        Action closeHandler,
        Action<UpdateInfo> showUpdateDownloadDialog) =>
        new(updateInfo, installedVersion, closeHandler, showUpdateDownloadDialog);
}

public interface IUpdateDownloadViewModelFactory
{
    UpdateDownloadViewModel Create(string assetUrl, Action closeHandler);
}

public sealed class UpdateDownloadViewModelFactory(
    IUpdateDownloader downloader,
    IInstallerLauncher launcher) : IUpdateDownloadViewModelFactory
{
    public UpdateDownloadViewModel Create(string assetUrl, Action closeHandler) =>
        new(assetUrl, downloader, launcher, closeHandler);
}
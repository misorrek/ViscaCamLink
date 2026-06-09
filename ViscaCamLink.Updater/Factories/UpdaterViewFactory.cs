namespace ViscaCamLink.Updater.Factories;

using ViscaCamLink.Updater.Common;
using ViscaCamLink.Updater.Manager;
using ViscaCamLink.Updater.ViewModels;
using ViscaCamLink.Updater.Views;

public class UpdaterViewFactory : IUpdaterViewFactory
{
    public UpdaterViewFactory(
        IDownloadManager downloadManager)
    {
        _downloadManager = downloadManager;
        //_zipExtractionManager = zipExtractionManager;
    }

    private readonly IDownloadManager _downloadManager;

    //private readonly IZipExtractionManager _zipExtractionManager;

    public UpdaterView CreateViewAndAttachViewModel(IApplicationUpdaterOptions updaterOptions, UpdateInfoEventArgs updateInfoEventArgs)
    {
        var viewModel = new UpdaterViewModel(_downloadManager, updaterOptions, updateInfoEventArgs);
        var view = new UpdaterView();
        
        viewModel.RequestCloseDialog = view.Close;
        view.DataContext = viewModel;

        return view;
    }
}

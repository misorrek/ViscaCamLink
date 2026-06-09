namespace ViscaCamLink.Updater.Factories;

using ViscaCamLink.Updater.Common;
using ViscaCamLink.Updater.Views;

public interface IUpdaterViewFactory
{
    UpdaterView CreateViewAndAttachViewModel(IApplicationUpdaterOptions updaterOptions, UpdateInfoEventArgs updateInfoEventArgs);
}

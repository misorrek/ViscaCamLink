namespace ViscaCamLink.Updater;

using System;

using ViscaCamLink.Updater.Common;

public interface IApplicationUpdater
{
    event EventHandler<UpdateInfoEventArgs>? CheckForUpdateEvent;
}

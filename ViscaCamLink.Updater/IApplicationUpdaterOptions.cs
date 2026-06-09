namespace ViscaCamLink.Updater;

using System;

public interface IApplicationUpdaterOptions
{        
    String UpdateXmlUrl { get; }

    Boolean RunSynchronous { get; }

    Int32 CheckForUpdateDelayInMilliseconds { get; }

    String DownloadPath { get; }
}

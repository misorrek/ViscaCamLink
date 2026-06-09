namespace ViscaCamLink.Updater.Factories;

using System;

using ViscaCamLink.Updater.Util;

public interface IApplicationUpdaterClientFactory
{
    public IApplicationUpdaterClient CreateClient(String baseAddress);
}

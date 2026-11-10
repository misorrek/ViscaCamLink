namespace ViscaCamLink;

using System;

using Velopack;

using ViscaCamLink.Infrastructure;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        using var singleInstance = new SingleInstanceManager();

        if (!singleInstance.IsPrimaryInstance)
        {
            return;
        }

        var application = new App();
        singleInstance.ActivationRequested += (_, _) =>
        {
            var dispatcher = application.Dispatcher;
            if (dispatcher.HasShutdownStarted || dispatcher.HasShutdownFinished)
            {
                return;
            }

            try
            {
                dispatcher.BeginInvoke(new Action(application.ActivateMainWindow));
            }
            catch (InvalidOperationException) when (dispatcher.HasShutdownStarted || dispatcher.HasShutdownFinished)
            {
            }
        };
        singleInstance.StartListening();

        // A downloaded but not yet applied update is applied here before the 
        // app starts (auto-apply is Velopack's default, set explicitly for clarity).
        VelopackApp.Build()
            .SetAutoApplyOnStartup(true)
            .Run();

        application.InitializeComponent();
        application.Run();
    }
}

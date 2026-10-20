namespace ViscaCamLink;

using System;

using Velopack;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // A downloaded but not yet applied update is applied here before the 
        // app starts (auto-apply is Velopack's default, set explicitly for clarity).
        VelopackApp.Build()
            .SetAutoApplyOnStartup(true)
            .Run();

        var application = new App();

        application.InitializeComponent();
        application.Run();
    }
}

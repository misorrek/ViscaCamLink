namespace ViscaCamLink;

using System;
using System.Diagnostics;

using Velopack;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        VelopackApp.Build()
            .OnAfterInstallFastCallback(_ =>
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = Environment.ProcessPath!,
                    UseShellExecute = true,
                });
            })
            .Run();

        var application = new App();

        application.InitializeComponent();
        application.Run();
    }
}

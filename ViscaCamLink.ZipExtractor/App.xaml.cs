namespace ViscaCamLink.ZipExtractor;

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;

using Microsoft.Extensions.Logging;

using ViscaCamLink.Common.Logging;
using ViscaCamLink.Common.Messaging;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private void Application_Startup(Object sender, StartupEventArgs startupEventArgs)
    {
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.ClearProviders();
            builder.AddProvider(new ViscaCamLinkLoggerProvider());
        });
        var logger = loggerFactory.CreateLogger("ZipExtraction");
        var messageHandler = new BaseMessageHandler(Application.Current.Dispatcher);

        try
        {
            var extractionOptions = new ZipExtractionOptions();
            var extractionManager = new ZipExtractionManager(messageHandler, logger);
            var extractionViewModel = new ZipExtractionViewModel(extractionOptions, extractionManager);
            var extractionView = new ZipExtractionView
            {
                DataContext = extractionViewModel,
                Topmost = true
            };

            extractionViewModel.RequestCloseDialog = () =>
            {
                extractionView.DialogResult = extractionViewModel.ExtractionSucceeded;
                extractionView.Close();
            };

            if (extractionView.ShowDialog() == true)
            {
                StartExtractedExe(extractionOptions, logger);
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unhandled error");
            messageHandler.ShowError("Interner Fehler", $"Erhaltene Fehlermeldung: {exception.Message}");
        }
        finally
        {
            Application.Current.Shutdown();
        }        
    }

    private static void StartExtractedExe(ZipExtractionOptions extractionOptions, ILogger logger)
    {
        const Int32 ErrorCode_Cancelled = 1223;

        var executablePath = String.IsNullOrWhiteSpace(extractionOptions.ExtractedExecutableFileName) 
            ? extractionOptions.OwnerExecutablePath 
            : Path.Combine(extractionOptions.OutputPath, extractionOptions.ExtractedExecutableFileName);        

        try
        {
            Process.Start(executablePath);
            logger.LogInformation("Successfully launched the extracted application \"{exe}\"", executablePath);
        }
        catch (Win32Exception exception)
            when (exception.NativeErrorCode != ErrorCode_Cancelled)
        {            
            logger.LogError(exception, "Error while launching the extracted application \"{exe}\"", executablePath);
        }       
    }
}

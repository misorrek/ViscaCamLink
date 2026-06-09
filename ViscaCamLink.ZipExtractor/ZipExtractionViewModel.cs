namespace ViscaCamLink.ZipExtractor;

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

using CommunityToolkit.Mvvm.Input;

using ViscaCamLink.Common;
using ViscaCamLink.ZipExtractor.Util;

public class ZipExtractionViewModel : BaseViewModel
{
    public ZipExtractionViewModel(
        ZipExtractionOptions extractionOptions, 
        ZipExtractionManager extractionManager)
    {
        _extractionOptions = extractionOptions;
        _extractionManager = extractionManager;

        _progressText = String.Empty;
        
        ExtractionCommand = new AsyncRelayCommand(RunExtraction);
        ExtractionRunning = false;
    }

    #region Commands

    public ICommand ExtractionCommand { get; }

    #endregion

    #region Bindable properties

    public Int32 ProgressInPercent
    {
        get => _progressInPercent;

        set
        {
            _progressInPercent = value;
            NotifyPropertyChanged();
        }
    }

    private Int32 _progressInPercent;

    public String ProgressText
    {
        get => _progressText;

        set
        {
            _progressText = value;
            NotifyPropertyChanged();
        }
    }

    private String _progressText;

    public Boolean ExtractionRunning { get; private set; }

    public Boolean ExtractionSucceeded { get; private set; }

    #endregion

    private readonly ZipExtractionOptions _extractionOptions;

    private readonly ZipExtractionManager _extractionManager;

    protected override Boolean CanCloseWindow()
    {
        return !ExtractionRunning;
    }

    private async Task RunExtraction()
    {
        ExtractionRunning = true;

        try
        {
            ExtractionSucceeded = await _extractionManager.ExtractAsync(
                _extractionOptions.InputPath,
                _extractionOptions.OutputPath,
                _extractionOptions.OwnerExecutablePath,
                _extractionOptions.ClearOutputPath,
                CreateProgress(),
                CancellationToken.None);

            ProgressText = "Entpacken vollständig";
        }
        finally 
        { 
            ExtractionRunning = false;
        }          
        
        RequestCloseDialog?.Invoke();
    }

    private IProgress<ZipExtractionReport> CreateProgress()
    {
        var progress = new Progress<ZipExtractionReport>();

        progress.ProgressChanged += (_, report) =>
        {
            ProgressInPercent = report.GetProgessInPercentIfChanged(ProgressInPercent);
            
            switch(report.State)
            {
                case ZipExtractionState.Extracting:
                    ProgressText = $"Extrahiere \"{report.CurrentEntityName}\"";
                    break;
                case ZipExtractionState.RemovingFile:
                    ProgressText = $"Entferne Datei \"{report.CurrentEntityName}\"";
                    break;
                case ZipExtractionState.RemovingDirectory:
                    ProgressText = $"Entferne Verzeichnis \"{report.CurrentEntityName}\"";
                    break;
                case ZipExtractionState.WaitingForApplication:
                    ProgressText = $"Warte auf Beendigung von ViscaCamLink";
                    break;
                default:
                    ProgressText = String.Empty;
                    break;
            }
        };

        return progress;
    }
}

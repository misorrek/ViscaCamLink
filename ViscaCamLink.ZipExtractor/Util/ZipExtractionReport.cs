namespace ViscaCamLink.ZipExtractor.Util;

using System;

public readonly struct ZipExtractionReport
{
    public ZipExtractionReport(
        ZipExtractionState state,
        String currentEntityName,
        Int32 progressInPercent)
    {
        State = state;        
        CurrentEntityName = currentEntityName;
        _progressInPercent = progressInPercent;
    }

    public ZipExtractionReport(
        ZipExtractionState state,
        String currentEntityName)
    {
        State = state;        
        CurrentEntityName = currentEntityName;
        _progressInPercent = ProgressUnchanged;
    }

    private const Int32 ProgressUnchanged = -1;

    public ZipExtractionState State { get; }    

    /// <summary>
    /// Could be a file, directory or process
    /// </summary>
    public String CurrentEntityName { get; }

    private readonly Int32 _progressInPercent;

    public Int32 GetProgessInPercentIfChanged(Int32 unchangedProgressInPercent)
    {
        return _progressInPercent == ProgressUnchanged ? unchangedProgressInPercent : _progressInPercent;
    }
}

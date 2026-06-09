namespace ViscaCamLink.ZipExtractor.Util;

public enum ZipExtractionState
{
    Extracting,
    RemovingFile,
    RemovingDirectory,
    WaitingForApplication,
}

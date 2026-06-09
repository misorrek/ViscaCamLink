namespace ViscaCamLink.ZipExtractor.Util;

using System.IO.Compression;

public static class ZipArchiveExtensions
{
    public static bool IsDirectory(this ZipArchiveEntry entry)
    {
        return string.IsNullOrEmpty(entry.Name) && (entry.FullName.EndsWith("/") || entry.FullName.EndsWith(@"\"));
    }
}

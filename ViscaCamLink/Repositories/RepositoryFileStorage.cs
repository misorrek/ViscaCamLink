namespace ViscaCamLink.Repositories;

using System;
using System.Globalization;
using System.IO;

using Microsoft.Extensions.Logging;

internal static partial class RepositoryFileStorage
{
    public static void WriteAtomically(string filePath, Action<Stream> write, ILogger logger)
    {
        var directory = Path.GetDirectoryName(filePath);
        var temporaryPath = $"{filePath}.{Guid.NewGuid():N}.tmp";

        try
        {
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (var stream = File.Create(temporaryPath))
            {
                write(stream);
            }

            if (File.Exists(filePath))
            {
                File.Copy(filePath, $"{filePath}.bak", overwrite: true);
            }

            File.Move(temporaryPath, filePath, overwrite: true);
        }
        finally
        {
            try
            {
                File.Delete(temporaryPath);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                LogTemporaryFileDeletionFailed(logger, exception, temporaryPath);
            }
        }
    }

    public static bool PreserveRejectedFile(string filePath, ILogger logger)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd'T'HHmmssfffffff'Z'", CultureInfo.InvariantCulture);
        var rejectedFilePath = $"{filePath}.corrupt-{timestamp}";

        try
        {
            File.Copy(filePath, rejectedFilePath, overwrite: false);
            LogRejectedFilePreserved(logger, filePath, rejectedFilePath);
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            LogRejectedFilePreservationFailed(logger, exception, filePath);
            return false;
        }
    }

    [LoggerMessage(EventId = 3301, Level = LogLevel.Warning, Message = "Temporary repository file '{FilePath}' could not be deleted")]
    private static partial void LogTemporaryFileDeletionFailed(ILogger logger, Exception exception, string filePath);

    [LoggerMessage(EventId = 3302, Level = LogLevel.Warning, Message = "Rejected repository file '{FilePath}' was preserved as '{RejectedFilePath}'")]
    private static partial void LogRejectedFilePreserved(ILogger logger, string filePath, string rejectedFilePath);

    [LoggerMessage(EventId = 3303, Level = LogLevel.Warning, Message = "Rejected repository file '{FilePath}' could not be preserved")]
    private static partial void LogRejectedFilePreservationFailed(ILogger logger, Exception exception, string filePath);
}

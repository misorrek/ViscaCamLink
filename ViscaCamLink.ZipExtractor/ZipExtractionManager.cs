namespace ViscaCamLink.ZipExtractor;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using ViscaCamLink.Common.Messaging;
using ViscaCamLink.ZipExtractor.Util;

public class ZipExtractionManager
{
    public ZipExtractionManager(
        IMessageHandler messageHandler, 
        ILogger logger)
    {
        _messageHandler = messageHandler;
        _logger = logger;
    }
    
    private const Int32 HResultErrorSharingViolation = 32;
    private const Int32 HResultErrorLockViolation = 33;
    private const Int32 DelayOnLockedFilesInMilliseconds = 5000;

    private readonly IMessageHandler _messageHandler;

    private readonly ILogger _logger;

    public async Task<Boolean> ExtractAsync(String zipFilePath, String extractionPath, String ownerExePath, Boolean clearExtractionPath, IProgress<ZipExtractionReport>? progress, CancellationToken cancellationToken)
    {
        if (String.IsNullOrEmpty(zipFilePath) || String.IsNullOrEmpty(extractionPath) || String.IsNullOrEmpty(ownerExePath)) 
        {
            return false;
        }

        if (!await CheckProcessesAndWaitForExit(ownerExePath, progress, cancellationToken).ConfigureAwait(false))
        {
            return false;
        }

        CheckPathForDirectorySeparator(ref extractionPath);

        using (var zipArchive = ZipFile.OpenRead(zipFilePath))
        {
            if (clearExtractionPath)
            {
                ClearPath(extractionPath, progress);
            }

            var progressInPercent = 0;

            for (var i = 0; i < zipArchive.Entries.Count; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                {                    
                    break;
                }
                var entry = zipArchive.Entries[i];

                progress?.Report(new ZipExtractionReport(ZipExtractionState.Extracting, entry.FullName, progressInPercent));

                if (!await CopyZipArchiveEntryToPath(entry, extractionPath, cancellationToken).ConfigureAwait(false))
                {
                    return false;
                }
                await Task.Delay(50); //TODO Remove

                progressInPercent = (i + 1) * 100 / zipArchive.Entries.Count;

                _logger.LogDebug("Extracting [{percent}%]. Current file \"{file}\".", progressInPercent, entry.FullName);
                progress?.Report(new ZipExtractionReport(ZipExtractionState.Extracting, entry.FullName, progressInPercent));
            }
        }

        return true;
    }

    private async Task<Boolean> CheckProcessesAndWaitForExit(String processFileName, IProgress<ZipExtractionReport>? progress, CancellationToken cancellationToken)
    {
        foreach (Process process in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(processFileName)))
        {
            try
            {
                if (process.MainModule is { FileName: not null } && process.MainModule.FileName.Equals(processFileName, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogDebug("Waiting for owner application process to exit...");
                    progress?.Report(new ZipExtractionReport(ZipExtractionState.WaitingForApplication, String.Empty));

                    await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception exception)
                when (exception is Win32Exception or SystemException)
            {
                _logger.LogError(exception, "Waiting for owner application process to exit failed.");
                _messageHandler.ShowError("Entpacken fehlgeschlagen", "Fehler beim Warten auf Beendigung von ViscaCamLink.");

                return false;
            }
        }

        return true;
    }

    private static void CheckPathForDirectorySeparator(ref String path)
    {
        if (!path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
        {
            path += Path.DirectorySeparatorChar;
        }
    }

    private void ClearPath(String path, IProgress<ZipExtractionReport>? progress)
    {
        _logger.LogDebug("Removing all files and folders from \"{path}\".", path);

        var directoryInfo = new DirectoryInfo(path);

        foreach (FileInfo file in directoryInfo.GetFiles())
        {
            _logger.LogDebug("Removing a file located at \"{fileName}\".", file.FullName);
            progress?.Report(new ZipExtractionReport(ZipExtractionState.RemovingFile, file.FullName));

            file.Delete();
        }

        foreach (DirectoryInfo directory in directoryInfo.GetDirectories())
        {
            _logger.LogDebug("Removing a directory located at \"{directoryName}\" and all its contents.", directory.FullName);
            progress?.Report(new ZipExtractionReport(ZipExtractionState.RemovingDirectory, directory.FullName));

            directory.Delete(true);
        }
    }

    private async Task<Boolean> CopyZipArchiveEntryToPath(ZipArchiveEntry zipArchiveEntry, String path, CancellationToken cancellationToken)
    {
        if (zipArchiveEntry.IsDirectory())
        {
            return true;
        }
        
        var copied = false;
        var filePath = Path.Combine(path, zipArchiveEntry.FullName.Replace('/', '\\'));
        var parentDirectory = Path.GetDirectoryName(filePath);

        if (parentDirectory == null)
        {
            _logger.LogError("Extraction path \"{filePath}\" has no parent directory, so it is invalid", filePath);
            _messageHandler.ShowError("Entpacken fehlgeschlagen", $"Der Zielpfad \"{filePath}\" ist ungültig, da er kein übergeordnetes Verzeichnis hat.");

            return false;
        }

        while (!copied)
        {            
            try
            {
                if (!Directory.Exists(parentDirectory))
                {
                    Directory.CreateDirectory(parentDirectory);
                }

                using (Stream destination = File.Open(filePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
                using (Stream entryStream = zipArchiveEntry.Open())
                {
                    await entryStream.CopyToAsync(destination, cancellationToken).ConfigureAwait(false);
                    destination.SetLength(destination.Position);
                }

                File.SetLastWriteTime(filePath, zipArchiveEntry.LastWriteTime.DateTime);

                copied = true;
            }
            catch (IOException exception)
                when ((exception.HResult & 0xFFFF) is HResultErrorSharingViolation or HResultErrorLockViolation)
            {
                _logger.LogError(exception, "\"{path}\" is locked. Waiting for five seconds.", filePath);

                await Task.Delay(DelayOnLockedFilesInMilliseconds, cancellationToken).ConfigureAwait(false);

                _logger.LogError(exception, "\"{path}\" is still locked. Checking who is locking...", filePath);

                List<Process>? lockingProcesses = null;

                try
                {
                    lockingProcesses = FileUtil.WhoIsLocking(filePath);
                }
                catch (Exception)
                {
                    _logger.LogError(exception, "Error while retrieving locking processes for \"{path}\"", filePath);
                }

                if (lockingProcesses == null)
                {
                    if (!ShowProcessLockedMessage(filePath))
                    {
                        throw new TaskCanceledException();
                    }

                    continue;
                }

                foreach (var lockingProcess in lockingProcesses)
                {
                    if (!ShowProcessLockedMessage(filePath, lockingProcess.ProcessName))
                    {
                        throw new TaskCanceledException();
                    }
                }
            }
        }

        return true;
    }

    private Boolean ShowProcessLockedMessage(String filePath, String? processName = null)
    {
        var messageBuilder = new StringBuilder();

        messageBuilder.Append(filePath);
        messageBuilder.Append(" ist gesperrt");

        if (!String.IsNullOrWhiteSpace(processName))
        {
            messageBuilder.Append(" von ");
            messageBuilder.Append(processName);
        }

        messageBuilder.Append(". Bitte beenden Sie den Prozess und wiederholen Sie den Vorgang.");

        return _messageHandler.ShowRetry(
            "Freigabe erforderlich",
            messageBuilder.ToString());
    }
}

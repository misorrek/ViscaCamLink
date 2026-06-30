namespace ViscaCamLink.Repositories;

using Microsoft.Extensions.Logging;

using ViscaCamLink.Util;

public sealed class AppSettings
{
    public LogLevel LogLevel { get; set; } = LogLevel.Information;

    public Language Language { get; set; } = Language.System;

    public string Ip { get; set; } = "192.168.0.1";

    public int Port { get; set; } = 5678;

    public bool MemoryContainerVisible { get; set; } = true;

    public bool MoveContainerVisible { get; set; } = true;

    public bool ZoomContainerVisible { get; set; } = true;

    public int PanTiltSpeed { get; set; } = 1;

    public int ZoomSpeed { get; set; } = 1;

    public bool NumpadLayout { get; set; } = true;
}
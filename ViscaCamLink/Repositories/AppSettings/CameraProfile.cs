namespace ViscaCamLink.Repositories.AppSettings;

using System;

using ViscaCamLink.Resources;

public class CameraProfile
{
    public const string DefaultIp = "192.168.0.1";
    public const int DefaultPort = 5678;

    private const int DefaultCameraProfileNumber = 1;

    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Format(Strings.CameraProfile_DefaultName, DefaultCameraProfileNumber);

    public string Ip { get; set; } = DefaultIp;

    public int Port { get; set; } = DefaultPort;
}

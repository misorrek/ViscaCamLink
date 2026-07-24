namespace ViscaCamLink.Repositories.AppSettings;

public class WindowPlacementData
{
    // 1 = SW_SHOWNORMAL (Win32 show command).
    public uint ShowCmd { get; set; } = 1;

    public int NormalLeft { get; set; }

    public int NormalTop { get; set; }

    public int NormalRight { get; set; }

    public int NormalBottom { get; set; }
}

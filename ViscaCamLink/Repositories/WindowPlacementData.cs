namespace ViscaCamLink.Repositories;

public sealed class WindowPlacementData
{
    public uint ShowCmd { get; set; } = 1; // SW_SHOWNORMAL

    public int NormalLeft { get; set; }

    public int NormalTop { get; set; }

    public int NormalRight { get; set; }

    public int NormalBottom { get; set; }
}

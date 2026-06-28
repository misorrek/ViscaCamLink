namespace ViscaCamLink.Visca;

public class ViscaProtocolException : Exception
{
    public ViscaProtocolException()
    {
    }

    public ViscaProtocolException(string message) : base(message)
    {
    }

    public ViscaProtocolException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

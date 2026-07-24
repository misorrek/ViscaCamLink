namespace ViscaCamLink.Visca;

using System;

public class ViscaResponseException : Exception
{
    public ViscaResponseException()
    {
    }

    public ViscaResponseException(string message) : base(message)
    {
    }

    public ViscaResponseException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

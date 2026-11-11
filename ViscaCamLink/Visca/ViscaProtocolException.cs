// Copyright 2020 Jon Skeet. All rights reserved.
// Adapted from jskeet/DemoCode (Apache-2.0); modified by Max Groiser.

namespace ViscaCamLink.Visca;

using System;

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

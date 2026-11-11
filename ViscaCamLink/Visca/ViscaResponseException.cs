// Copyright 2020 Jon Skeet. All rights reserved.
// Adapted from jskeet/DemoCode (Apache-2.0); modified by Max Groiser.

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

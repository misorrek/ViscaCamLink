// Copyright 2020 Jon Skeet. All rights reserved.
// Adapted from jskeet/DemoCode (Apache-2.0); modified by Max Groiser.

namespace ViscaCamLink.Visca.Types;

public enum PowerStatus
{
    Unknown = 1,
    On = 2,
    Standby = 3,
    InternalPowerCircuitError = 4
}

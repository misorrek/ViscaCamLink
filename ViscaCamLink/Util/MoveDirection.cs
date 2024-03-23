namespace ViscaCamLink.Util;

using System;

[Flags]
public enum MoveDirection
{
    None = 0,
    Left = 1,
    Up = 2,
    Right = 4,
    Down = 8,
    LeftUp = Left | Up,
    LeftDown = Left | Down,
    RightUp = Right | Up,
    RightDown = Right | Down
}

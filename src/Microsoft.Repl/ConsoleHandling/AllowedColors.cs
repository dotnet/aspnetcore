// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Repl.ConsoleHandling
{
    [Flags]
    public enum AllowedColors
    {
        Black = 0x00,
        BoldBlack = Bold | Black,
        Red = 0x01,
        BoldRed = Bold | Red,
        Green = 0x02,
        BoldGreen = Bold | Green,
        Yellow = 0x03,
        BoldYellow = Bold | Yellow,
        Blue = 0x04,
        BoldBlue = Bold | Blue,
        Magenta = 0x05,
        BoldMagenta = Bold | Magenta,
        Cyan = 0x06,
        BoldCyan = Bold | Cyan,
        White = 0x07,
        BoldWhite = White | Bold,
        Bold = 0x100,
        None = 0x99
    }
}

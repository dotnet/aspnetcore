// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace BasicTestApp.InteropTest;

[StructLayout(LayoutKind.Explicit)]
public struct InteropStruct
{
    [FieldOffset(0)]
    public string Message;

    [FieldOffset(8)]
    public int NumberField;
}

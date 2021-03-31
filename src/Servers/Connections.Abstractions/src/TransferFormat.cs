// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Connections
{
    /// <summary>
    /// Represents the possible transfer formats.
    /// </summary>
    [Flags]
    public enum TransferFormat
    {
        Binary = 0x01,
        Text = 0x02
    }
}

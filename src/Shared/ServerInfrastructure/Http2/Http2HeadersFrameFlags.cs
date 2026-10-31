// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;

[Flags]
internal enum Http2HeadersFrameFlags : byte
{
    NONE = 0x0,
    END_STREAM = 0x1,
    END_HEADERS = 0x4,
    PADDED = 0x8,
    PRIORITY = 0x20
}

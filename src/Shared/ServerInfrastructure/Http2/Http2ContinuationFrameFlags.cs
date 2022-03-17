// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;

[Flags]
internal enum Http2ContinuationFrameFlags : byte
{
    NONE = 0x0,
    END_HEADERS = 0x4,
}

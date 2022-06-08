// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3;

internal enum Http3StreamType : long
{
    Control = 0x00,
    Push = 0x01,
    Encoder = 0x02,
    Decoder = 0x03,
    WebTransportUnidirectional = 0x54,
    WebTransportBidirectional = 0x41
}

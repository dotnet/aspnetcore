// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.DirectSsl.Connection;

/// <summary>
/// The options for configuring a <see cref="DirectSslConnectionContext"/>.
/// </summary>
internal class DirectSslConnectionContextFactoryOptions
{
    private readonly DirectSslTransportOptions _transportOptions;

    public DirectSslConnectionContextFactoryOptions(DirectSslTransportOptions transportOptions)
    {
        _transportOptions = transportOptions;
    }
}

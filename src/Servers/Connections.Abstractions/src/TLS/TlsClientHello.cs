// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Connections.Abstractions.TLS;

public struct TLS_CLIENT_HELLO
{
    public SslProtocols ProtocolVersion;  // Version of the TLS protocol

    public override string ToString()
    {
        return $"""
        TLS CLIENT HELLO MESSAGE:
        - ProtocolVersion: {ProtocolVersion}
        """;
    }
}

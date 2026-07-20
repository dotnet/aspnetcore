// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.DirectTls;

internal class TlsException : Exception
{
    public TlsException(string message, Exception ex) : base(message, ex) { }

    public TlsException(string message) : base(message) { }
    public TlsException(int error) : base($"TLS error: {error}") { }
}
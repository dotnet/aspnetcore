// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.DirectSsl;

internal class SslException : Exception
{
    public SslException(string message, Exception ex) : base(message, ex) { }

    public SslException(string message) : base(message) { }
    public SslException(int error) : base($"SSL error: {error}") { }
}
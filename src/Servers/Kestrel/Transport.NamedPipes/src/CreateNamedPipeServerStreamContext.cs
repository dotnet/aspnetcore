// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Pipes;
using Microsoft.AspNetCore.Connections;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.NamedPipes;

/// <summary>
/// Provides information about an endpoint when creating a <see cref="NamedPipeServerStream"/>.
/// </summary>
public sealed class CreateNamedPipeServerStreamContext
{
    /// <summary>
    /// Gets the endpoint.
    /// </summary>
    public required NamedPipeEndPoint NamedPipeEndPoint { get; init; }
    /// <summary>
    /// Gets the pipe options.
    /// </summary>
    public required PipeOptions PipeOptions { get; init; }
    /// <summary>
    /// Gets the default access control and audit security.
    /// </summary>
    public PipeSecurity? PipeSecurity { get; init; }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.IO.Pipes;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.NamedPipes;

/// <summary>
/// Options for named pipe based transports.
/// </summary>
public sealed class NamedPipeTransportOptions
{
    /// <summary>
    /// 
    /// </summary>
    public int AcceptQueueCount { get; set; } = Math.Clamp(Environment.ProcessorCount, 1, 16);

    /// <summary>
    /// Gets or sets the maximum unconsumed incoming bytes the transport will buffer.
    /// <para>
    /// A value of <see langword="null"/> or 0 disables backpressure entirely allowing unlimited buffering.
    /// Unlimited server buffering is a security risk given untrusted clients.
    /// </para>
    /// </summary>
    /// <remarks>
    /// Defaults to 1 MiB.
    /// </remarks>
    public long? MaxReadBufferSize { get; set; } = 1024 * 1024;

    /// <summary>
    /// Gets or sets the maximum outgoing bytes the transport will buffer before applying write backpressure.
    /// <para>
    /// A value of <see langword="null"/> or 0 disables backpressure entirely allowing unlimited buffering.
    /// Unlimited server buffering is a security risk given untrusted clients.
    /// </para>
    /// </summary>
    /// <remarks>
    /// Defaults to 64 KiB.
    /// </remarks>
    public long? MaxWriteBufferSize { get; set; } = 64 * 1024;

    /// <summary>
    /// Gets or sets a value that indicates that the pipe can only be connected to by a client created by
    /// the same user account.
    /// <para>
    /// On Windows, a value of true verifies both the user account and elevation level.
    /// </para>
    /// </summary>
    /// <remarks>
    /// Defaults to true.
    /// </remarks>
    public bool CurrentUserOnly { get; set; } = true;

    /// <summary>
    /// Gets or sets the security information that determines the access control and audit security for pipes.
    /// </summary>
    public PipeSecurity? PipeSecurity { get; set; }

    internal Func<MemoryPool<byte>> MemoryPoolFactory { get; set; } = PinnedBlockMemoryPoolFactory.Create;
}

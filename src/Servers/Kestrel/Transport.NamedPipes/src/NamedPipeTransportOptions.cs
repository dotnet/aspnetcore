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
    /// The number of listener queues used to accept name pipe connections.
    /// </summary>
    /// <remarks>
    /// Defaults to <see cref="Environment.ProcessorCount" /> rounded down and clamped between 1 and 16.
    /// </remarks>
    public int ListenerQueueCount { get; set; } = Math.Min(Environment.ProcessorCount, 16);

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
    /// Gets or sets the security information that determines the default access control and audit security for pipes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Defaults to <c>null</c>, which is no pipe security.
    /// </para>
    /// <para>
    /// Configuring <see cref="PipeSecurity"/> sets the default access control and audit security for pipes.
    /// If per-endpoint security is needed then <see cref="CreateNamedPipeServerStream"/> can be configured
    /// to create streams with different security settings.</para>
    /// </remarks>
    public PipeSecurity? PipeSecurity { get; set; }

    /// <summary>
    /// A function used to create a new <see cref="NamedPipeServerStream"/> to listen with. If
    /// not set, <see cref="CreateDefaultNamedPipeServerStream" /> is used.
    /// </summary>
    /// <remarks>
    /// Defaults to <see cref="CreateDefaultNamedPipeServerStream"/>.
    /// </remarks>
    public Func<CreateNamedPipeServerStreamContext, NamedPipeServerStream> CreateNamedPipeServerStream { get; set; } = CreateDefaultNamedPipeServerStream;

    /// <summary>
    /// Creates a default instance of <see cref="NamedPipeServerStream"/> for the given
    /// <see cref="CreateNamedPipeServerStreamContext"/> that can be used by a connection listener
    /// to listen for inbound requests.
    /// </summary>
    /// <param name="context">A <see cref="CreateNamedPipeServerStreamContext"/>.</param>
    /// <returns>
    /// A <see cref="NamedPipeServerStream"/> instance.
    /// </returns>
    public static NamedPipeServerStream CreateDefaultNamedPipeServerStream(CreateNamedPipeServerStreamContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.PipeSecurity != null)
        {
            return NamedPipeServerStreamAcl.Create(
                context.NamedPipeEndPoint.PipeName,
                PipeDirection.InOut,
                NamedPipeServerStream.MaxAllowedServerInstances,
                PipeTransmissionMode.Byte,
                context.PipeOptions,
                inBufferSize: 0, // Buffer in System.IO.Pipelines
                outBufferSize: 0, // Buffer in System.IO.Pipelines
                context.PipeSecurity);
        }
        else
        {
            return new NamedPipeServerStream(
                context.NamedPipeEndPoint.PipeName,
                PipeDirection.InOut,
                NamedPipeServerStream.MaxAllowedServerInstances,
                PipeTransmissionMode.Byte,
                context.PipeOptions,
                inBufferSize: 0,
                outBufferSize: 0);
        }
    }

    internal Func<MemoryPool<byte>> MemoryPoolFactory { get; set; } = PinnedBlockMemoryPoolFactory.Create;
}

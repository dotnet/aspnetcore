// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;

/// <summary>
/// Options for <see cref="SocketConnectionContextFactory"/>.
/// </summary>
public class SocketConnectionFactoryOptions
{
    /// <summary>
    /// Create a new instance.
    /// </summary>
    public SocketConnectionFactoryOptions() { }

    internal SocketConnectionFactoryOptions(SocketTransportOptions transportOptions)
    {
        IOQueueCount = transportOptions.IOQueueCount;
        WaitForDataBeforeAllocatingBuffer = transportOptions.WaitForDataBeforeAllocatingBuffer;
        MaxReadBufferSize = transportOptions.MaxReadBufferSize;
        MaxWriteBufferSize = transportOptions.MaxWriteBufferSize;
        UnsafePreferInlineScheduling = transportOptions.UnsafePreferInlineScheduling;
        MemoryPoolFactory = transportOptions.MemoryPoolFactory;
        FinOnError = transportOptions.FinOnError;
    }

    // Opt-out flag for back compat. Remove in 9.0 (or make public).
    internal bool FinOnError { get; set; }

    /// <summary>
    /// The number of I/O queues used to process requests. Set to 0 to directly schedule I/O to the ThreadPool.
    /// </summary>
    /// <remarks>
    /// Defaults to a value based on and limited to <see cref="Environment.ProcessorCount" />.
    /// </remarks>
    public int IOQueueCount { get; set; } = Internal.IOQueue.DefaultCount;

    /// <summary>
    /// Wait until there is data available to allocate a buffer. Setting this to false can increase throughput at the cost of increased memory usage.
    /// </summary>
    /// <remarks>
    /// Defaults to true.
    /// </remarks>
    public bool WaitForDataBeforeAllocatingBuffer { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum unconsumed incoming bytes the transport will buffer.
    /// </summary>
    public long? MaxReadBufferSize { get; set; } = 1024 * 1024;

    /// <summary>
    /// Gets or sets the maximum outgoing bytes the transport will buffer before applying write backpressure.
    /// </summary>
    public long? MaxWriteBufferSize { get; set; } = 64 * 1024;

    /// <summary>
    /// Inline application and transport continuations instead of dispatching to the threadpool.
    /// </summary>
    /// <remarks>
    /// This will run application code on the IO thread which is why this is unsafe.
    /// It is recommended to set the DOTNET_SYSTEM_NET_SOCKETS_INLINE_COMPLETIONS environment variable to '1' when using this setting to also inline the completions
    /// at the runtime layer as well.
    /// This setting can make performance worse if there is expensive work that will end up holding onto the IO thread for longer than needed.
    /// Test to make sure this setting helps performance.
    /// </remarks>
    public bool UnsafePreferInlineScheduling { get; set; }

    internal Func<MemoryPool<byte>> MemoryPoolFactory { get; set; } = PinnedBlockMemoryPoolFactory.Create;
}

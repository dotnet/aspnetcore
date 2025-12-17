// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.DirectSsl.Internal;

internal sealed class DirectSocketConnectionFactoryOptions
{
    public DirectSocketConnectionFactoryOptions(DirectSocketTransportOptions options)
    {
        MaxReadBufferSize = options.MaxReadBufferSize;
        MaxWriteBufferSize = options.MaxWriteBufferSize;
        WaitForDataBeforeAllocatingBuffer = options.WaitForDataBeforeAllocatingBuffer;
        UnsafePreferInlineScheduling = options.UnsafePreferInlineScheduling;
        FinOnError = options.FinOnError;
        IOQueueCount = options.IOQueueCount;
        MemoryPoolFactory = options.MemoryPoolFactory;
    }

    public long? MaxReadBufferSize { get; }
    public long? MaxWriteBufferSize { get; }
    public bool WaitForDataBeforeAllocatingBuffer { get; }
    public bool UnsafePreferInlineScheduling { get; }
    public bool FinOnError { get; }
    public int IOQueueCount { get; }
    public IMemoryPoolFactory<byte>? MemoryPoolFactory { get; }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

internal abstract class WriteOnlyStream : Stream
{
    public override bool CanRead => false;

    public override bool CanWrite => true;

    public override int ReadTimeout
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count)
        => throw new NotSupportedException();

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
      => throw new NotSupportedException();

    public override ValueTask<int> ReadAsync(Memory<byte> memory, CancellationToken cancellationToken)
        => throw new NotSupportedException();
}

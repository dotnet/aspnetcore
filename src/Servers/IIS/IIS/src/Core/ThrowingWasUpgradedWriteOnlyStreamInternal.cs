// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Server.IIS.Core;

/// <summary>
/// A <see cref="Stream"/> which throws on calls to write after the stream was upgraded
/// </summary>
/// <remarks>
/// Users should not need to instantiate this class.
/// </remarks>
internal sealed class ThrowingWasUpgradedWriteOnlyStreamInternal : WriteOnlyStreamInternal
{
    ///<inheritdoc/>
    public override void Write(byte[] buffer, int offset, int count)
        => throw new InvalidOperationException(CoreStrings.ResponseStreamWasUpgraded);

    ///<inheritdoc/>
    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        => throw new InvalidOperationException(CoreStrings.ResponseStreamWasUpgraded);

    ///<inheritdoc/>
    public override void Flush()
        => throw new InvalidOperationException(CoreStrings.ResponseStreamWasUpgraded);

    ///<inheritdoc/>
    public override long Seek(long offset, SeekOrigin origin)
        => throw new NotSupportedException();

    ///<inheritdoc/>
    public override void SetLength(long value)
        => throw new NotSupportedException();
}

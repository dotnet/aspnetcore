// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Server.IIS.Core
{
    public class ThrowingWasUpgradedWriteOnlyStream : WriteOnlyStream
    {
        public override void Write(byte[] buffer, int offset, int count)
            => throw new InvalidOperationException(CoreStrings.ResponseStreamWasUpgraded);

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => throw new InvalidOperationException(CoreStrings.ResponseStreamWasUpgraded);

        public override void Flush()
            => throw new InvalidOperationException(CoreStrings.ResponseStreamWasUpgraded);

        public override long Seek(long offset, SeekOrigin origin)
            => throw new NotSupportedException();

        public override void SetLength(long value)
            => throw new NotSupportedException();
    }
}

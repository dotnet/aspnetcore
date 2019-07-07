// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.WebUtilities
{
    public interface IBufferedWriteStream : IAsyncDisposable, IDisposable
    {
        Stream Buffer { get; }
        Task DrainBufferAsync(Stream destination, CancellationToken cancellationToken = default);
    }
}

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http
{
    public interface IHttpOutputProducer
    {
        Task WriteAsync<T>(Func<PipeWriter, T, long> callback, T state, CancellationToken cancellationToken);
        Task FlushAsync(CancellationToken cancellationToken);
        Task Write100ContinueAsync();
        void WriteResponseHeaders(int statusCode, string ReasonPhrase, HttpResponseHeaders responseHeaders);
        // This takes ReadOnlySpan instead of ReadOnlyMemory because it always synchronously copies data before flushing.
        Task WriteDataAsync(ReadOnlySpan<byte> data, CancellationToken cancellationToken);
        Task WriteStreamSuffixAsync();
    }
}

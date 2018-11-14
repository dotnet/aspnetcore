// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http
{
    public interface IHttpOutputProducer : IDisposable
    {
        void Abort(ConnectionAbortedException abortReason);
        Task WriteAsync<T>(Func<PipeWriter, T, long> callback, T state);
        Task FlushAsync(CancellationToken cancellationToken);
        Task Write100ContinueAsync(CancellationToken cancellationToken);
        void WriteResponseHeaders(int statusCode, string ReasonPhrase, HttpResponseHeaders responseHeaders);
        // The reason this is ReadOnlySpan and not ReadOnlyMemory is because writes are always
        // synchronous. Flushing to get back pressure is the only time we truly go async but
        // that's after the buffer is copied
        Task WriteDataAsync(ReadOnlySpan<byte> data, CancellationToken cancellationToken);
        Task WriteStreamSuffixAsync(CancellationToken cancellationToken);
    }
}

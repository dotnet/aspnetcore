// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http
{
    public interface IHttpOutputProducer : IDisposable
    {
        void Abort(Exception error);
        Task WriteAsync<T>(Action<PipeWriter, T> callback, T state);
        Task FlushAsync(CancellationToken cancellationToken);
        Task Write100ContinueAsync(CancellationToken cancellationToken);
        void WriteResponseHeaders(int statusCode, string ReasonPhrase, HttpResponseHeaders responseHeaders);
        Task WriteDataAsync(ArraySegment<byte> data, CancellationToken cancellationToken);
        Task WriteStreamSuffixAsync(CancellationToken cancellationToken);
    }
}

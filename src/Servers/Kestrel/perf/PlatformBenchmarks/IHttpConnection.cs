// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO.Pipelines;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;

namespace PlatformBenchmarks
{
    public interface IHttpConnection : IHttpHeadersHandler, IHttpRequestLineHandler
    {
        PipeReader Reader { get; set; }
        PipeWriter Writer { get; set; }
        Task ExecuteAsync();
        ValueTask OnReadCompletedAsync();
    }
}

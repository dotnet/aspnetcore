// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Microsoft.DotNet.OpenApi
{
    public interface IHttpResponseMessageWrapper : IDisposable
    {
        Task<Stream> Stream { get; }
        ContentDispositionHeaderValue ContentDisposition();
        HttpStatusCode StatusCode { get; }
        bool IsSuccessCode();
    }
}

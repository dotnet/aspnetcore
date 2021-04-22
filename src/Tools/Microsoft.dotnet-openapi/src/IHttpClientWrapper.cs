// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.DotNet.OpenApi;

namespace Microsoft.DotNet.Openapi.Tools
{
    internal interface IHttpClientWrapper : IDisposable
    {
        Task<IHttpResponseMessageWrapper> GetResponseAsync(string url);
    }
}

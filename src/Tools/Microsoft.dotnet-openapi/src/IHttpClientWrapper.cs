// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.DotNet.OpenApi;

namespace Microsoft.DotNet.Openapi.Tools;

internal interface IHttpClientWrapper : IDisposable
{
    Task<IHttpResponseMessageWrapper> GetResponseAsync(string url);
}

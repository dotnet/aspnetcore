// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Openapi.Tools
{
    internal interface IHttpClientWrapper : IDisposable
    {
        Task<Stream> GetStreamAsync(string url);
    }
}

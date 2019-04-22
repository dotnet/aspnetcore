// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Openapi.Tools
{
    public class HttpClientWrapper : IHttpClientWrapper
    {
        private readonly HttpClient _client;

        public HttpClientWrapper(HttpClient client)
        {
            _client = client;
        }

        public void Dispose()
        {
            _client.Dispose();
        }

        public Task<Stream> GetStreamAsync(string url)
        {
            return _client.GetStreamAsync(url);
        }
    }
}

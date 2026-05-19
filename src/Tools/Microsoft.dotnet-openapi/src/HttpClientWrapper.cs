// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.DotNet.OpenApi;
using Microsoft.DotNet.OpenApi.Commands;

namespace Microsoft.DotNet.Openapi.Tools;

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

    public async Task<IHttpResponseMessageWrapper> GetResponseAsync(string url)
    {
        var response = await _client.GetAsync(url);

        return new HttpResponseMessageWrapper(response);
    }

    public Task<Stream> GetStreamAsync(string url)
    {
        return _client.GetStreamAsync(url);
    }
}

public class HttpResponseMessageWrapper : IHttpResponseMessageWrapper
{
    private readonly HttpResponseMessage _response;

    public HttpResponseMessageWrapper(HttpResponseMessage response)
    {
        _response = response;
    }

    public Task<Stream> Stream => _response.Content.ReadAsStreamAsync();

    public HttpStatusCode StatusCode => _response.StatusCode;

    public bool IsSuccessCode() => _response.IsSuccessStatusCode;

    public ContentDispositionHeaderValue ContentDisposition()
    {
        if (_response.Headers.TryGetValues(BaseCommand.ContentDispositionHeaderName, out var disposition))
        {
            return new ContentDispositionHeaderValue(disposition.First());
        }

        return null;
    }

    public void Dispose()
    {
        _response.Dispose();
    }
}

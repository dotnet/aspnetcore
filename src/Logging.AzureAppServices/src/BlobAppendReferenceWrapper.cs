// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Logging.AzureAppServices;

/// <inheritdoc />
internal sealed class BlobAppendReferenceWrapper : ICloudAppendBlob
{
    private readonly Uri _fullUri;
    private readonly HttpClient _client;
    private readonly Uri _appendUri;

    public BlobAppendReferenceWrapper(string containerUrl, string name, HttpClient client)
    {
        var uriBuilder = new UriBuilder(containerUrl);
        uriBuilder.Path += "/" + name;
        _fullUri = uriBuilder.Uri;

        AppendBlockQuery(uriBuilder);
        _appendUri = uriBuilder.Uri;
        _client = client;
    }

    /// <inheritdoc />
    public async Task AppendAsync(ArraySegment<byte> data, CancellationToken cancellationToken)
    {
        Task<HttpResponseMessage> AppendDataAsync()
        {
            var message = new HttpRequestMessage(HttpMethod.Put, _appendUri)
            {
                Content = new ByteArrayContent(data.Array, data.Offset, data.Count)
            };
            AddCommonHeaders(message);

            return _client.SendAsync(message, cancellationToken);
        }

        var response = await AppendDataAsync().ConfigureAwait(false);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            // If no blob exists try creating it
            var message = new HttpRequestMessage(HttpMethod.Put, _fullUri)
            {
                // Set Content-Length to 0 to create "Append Blob"
                Content = new ByteArrayContent(Array.Empty<byte>()),
                Headers =
                {
                    { "If-None-Match", "*" }
                }
            };

            AddCommonHeaders(message);

            response = await _client.SendAsync(message, cancellationToken).ConfigureAwait(false);

            // If result is 2** or 412 try to append again
            if (response.IsSuccessStatusCode ||
                response.StatusCode == HttpStatusCode.PreconditionFailed)
            {
                // Retry sending data after blob creation
                response = await AppendDataAsync().ConfigureAwait(false);
            }
        }

        response.EnsureSuccessStatusCode();
    }

    private static void AddCommonHeaders(HttpRequestMessage message)
    {
        message.Headers.Add("x-ms-blob-type", "AppendBlob");
        message.Headers.Add("x-ms-version", "2016-05-31");
        message.Headers.Date = DateTimeOffset.UtcNow;
    }

    private static void AppendBlockQuery(UriBuilder uriBuilder)
    {
        // See https://msdn.microsoft.com/en-us/library/system.uribuilder.query.aspx for:
        // Note: Do not append a string directly to Query property.
        // If the length of Query is greater than 1, retrieve the property value
        // as a string, remove the leading question mark, append the new query string,
        // and set the property with the combined string.
        var queryToAppend = "comp=appendblock";
        if (uriBuilder.Query != null && uriBuilder.Query.Length > 1)
        {
#if NETFRAMEWORK || NETSTANDARD
            uriBuilder.Query = uriBuilder.Query.Substring(1) + "&" + queryToAppend;
#else
            uriBuilder.Query = string.Concat(uriBuilder.Query.AsSpan(1), "&", queryToAppend);
#endif
        }
        else
        {
            uriBuilder.Query = queryToAppend;
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.InternalTesting;

namespace Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests.TestTransport;

/// <summary>
/// Lightweight version of HttpClient implemented on top of an arbitrary Stream.
/// </summary>
internal class InMemoryHttpClientSlim
{
    private readonly TestServer _inMemoryTestServer;

    public InMemoryHttpClientSlim(TestServer testServer)
    {
        _inMemoryTestServer = testServer;
    }

    public async Task<string> GetStringAsync(string requestUri, bool validateCertificate = true)
        => await GetStringAsync(new Uri(requestUri), validateCertificate).ConfigureAwait(false);

    public async Task<string> GetStringAsync(Uri requestUri, bool validateCertificate = true)
    {
        using (var connection = _inMemoryTestServer.CreateConnection())
        using (var stream = await GetStream(connection.Stream, requestUri, validateCertificate).ConfigureAwait(false))
        {
            using (var writer = new StreamWriter(stream, Encoding.ASCII, bufferSize: 1024, leaveOpen: true))
            {
                await writer.WriteAsync($"GET {requestUri.PathAndQuery} HTTP/1.0\r\n").ConfigureAwait(false);
                await writer.WriteAsync($"Host: {GetHost(requestUri)}\r\n").ConfigureAwait(false);
                await writer.WriteAsync("\r\n").ConfigureAwait(false);
            }

            return await ReadResponse(stream).ConfigureAwait(false);
        }
    }

    internal static string GetHost(Uri requestUri)
    {
        var authority = requestUri.Authority;
        if (requestUri.HostNameType == UriHostNameType.IPv6)
        {
            // Make sure there's no % scope id. https://github.com/aspnet/KestrelHttpServer/issues/2637
            var address = IPAddress.Parse(requestUri.Host);
            address = new IPAddress(address.GetAddressBytes()); // Drop scope Id.
            if (requestUri.IsDefaultPort)
            {
                authority = $"[{address}]";
            }
            else
            {
                authority = $"[{address}]:{requestUri.Port.ToString(CultureInfo.InvariantCulture)}";
            }
        }
        return authority;
    }

    public async Task<string> PostAsync(string requestUri, HttpContent content, bool validateCertificate = true)
        => await PostAsync(new Uri(requestUri), content, validateCertificate).ConfigureAwait(false);

    public async Task<string> PostAsync(Uri requestUri, HttpContent content, bool validateCertificate = true)
    {
        using (var connection = _inMemoryTestServer.CreateConnection())
        using (var stream = await GetStream(connection.Stream, requestUri, validateCertificate).ConfigureAwait(false))
        {
            using (var writer = new StreamWriter(stream, Encoding.ASCII, bufferSize: 1024, leaveOpen: true))
            {
                await writer.WriteAsync($"POST {requestUri.PathAndQuery} HTTP/1.0\r\n").ConfigureAwait(false);
                await writer.WriteAsync($"Host: {requestUri.Authority}\r\n").ConfigureAwait(false);
                await writer.WriteAsync($"Content-Type: {content.Headers.ContentType}\r\n").ConfigureAwait(false);
                await writer.WriteAsync($"Content-Length: {content.Headers.ContentLength}\r\n").ConfigureAwait(false);
                await writer.WriteAsync("\r\n").ConfigureAwait(false);
            }

            await content.CopyToAsync(stream).ConfigureAwait(false);

            return await ReadResponse(stream).ConfigureAwait(false);
        }
    }

    private static async Task<string> ReadResponse(Stream stream)
    {
        using (var reader = new StreamReader(stream, Encoding.ASCII, detectEncodingFromByteOrderMarks: true,
            bufferSize: 1024, leaveOpen: true))
        {
            var response = await reader.ReadToEndAsync().DefaultTimeout().ConfigureAwait(false);

            var status = GetStatus(response);
            new HttpResponseMessage(status).EnsureSuccessStatusCode();

            var body = response.Substring(response.IndexOf("\r\n\r\n", StringComparison.Ordinal) + 4);
            return body;
        }
    }

    private static HttpStatusCode GetStatus(string response)
    {
        var statusStart = response.IndexOf(' ') + 1;
        var statusEnd = response.IndexOf(' ', statusStart) - 1;
        var statusLength = statusEnd - statusStart + 1;

        if (statusLength < 1)
        {
            throw new InvalidDataException($"No StatusCode found in '{response}'");
        }

        return (HttpStatusCode)int.Parse(response.Substring(statusStart, statusLength), CultureInfo.InvariantCulture);
    }

    private static async Task<Stream> GetStream(Stream rawStream, Uri requestUri, bool validateCertificate)
    {
        if (requestUri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
        {
            var sslStream = new SslStream(rawStream, leaveInnerStreamOpen: false, userCertificateValidationCallback:
                validateCertificate ? null : (RemoteCertificateValidationCallback)((a, b, c, d) => true));

            await sslStream.AuthenticateAsClientAsync(requestUri.Host, clientCertificates: null,
                enabledSslProtocols: SslProtocols.None,
                checkCertificateRevocation: validateCertificate).ConfigureAwait(false);
            return sslStream;
        }
        else
        {
            return rawStream;
        }
    }
}

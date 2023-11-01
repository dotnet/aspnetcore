// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.InternalTesting;

/// <summary>
/// Lightweight version of HttpClient implemented using Socket and SslStream.
/// </summary>
public static class HttpClientSlim
{
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static async Task<string> GetStringAsync(string requestUri, bool validateCertificate = true)
        => await GetStringAsync(new Uri(requestUri), validateCertificate).ConfigureAwait(false);

    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static async Task<string> GetStringAsync(Uri requestUri, bool validateCertificate = true)
    {
        return await RetryRequest(async () =>
        {
            using (var stream = await GetStream(requestUri, validateCertificate).ConfigureAwait(false))
            {
                using (var writer = new StreamWriter(stream, Encoding.ASCII, bufferSize: 1024, leaveOpen: true))
                {
                    await writer.WriteAsync($"GET {requestUri.PathAndQuery} HTTP/1.0\r\n").ConfigureAwait(false);
                    await writer.WriteAsync($"Host: {GetHost(requestUri)}\r\n").ConfigureAwait(false);
                    await writer.WriteAsync("\r\n").ConfigureAwait(false);
                }

                return await ReadResponse(stream).ConfigureAwait(false);
            }
        }).ConfigureAwait(false);
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

    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static async Task<string> PostAsync(string requestUri, HttpContent content, bool validateCertificate = true)
        => await PostAsync(new Uri(requestUri), content, validateCertificate).ConfigureAwait(false);

    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static async Task<string> PostAsync(Uri requestUri, HttpContent content, bool validateCertificate = true)
    {
        return await RetryRequest(async () =>
        {
            using (var stream = await GetStream(requestUri, validateCertificate).ConfigureAwait(false))
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
        }).ConfigureAwait(false);
    }

    private static async Task<string> ReadResponse(Stream stream)
    {
        using (var reader = new StreamReader(stream, Encoding.ASCII, detectEncodingFromByteOrderMarks: true,
            bufferSize: 1024, leaveOpen: true))
        {
            var response = await reader.ReadToEndAsync().ConfigureAwait(false);

            var status = GetStatus(response);
            new HttpResponseMessage(status).EnsureSuccessStatusCode();

            var body = response.Substring(response.IndexOf("\r\n\r\n", StringComparison.Ordinal) + 4);
            return body;
        }
    }

    private static async Task<string> RetryRequest(Func<Task<string>> retryBlock)
    {
        var retryCount = 1;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            retryCount = 3;
        }

        for (var retry = 0; retry < retryCount; retry++)
        {
            try
            {
                return await retryBlock().ConfigureAwait(false);
            }
            catch (InvalidDataException)
            {
                if (retry == retryCount - 1)
                {
                    throw;
                }
            }
        }

        // This will never be hit.
        throw new NotSupportedException();
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

#if NETSTANDARD2_0 || NETFRAMEWORK
        return (HttpStatusCode)int.Parse(response.Substring(statusStart, statusLength), CultureInfo.InvariantCulture);
#else
        return (HttpStatusCode)int.Parse(response.AsSpan(statusStart, statusLength), CultureInfo.InvariantCulture);
#endif
    }

    private static async Task<Stream> GetStream(Uri requestUri, bool validateCertificate)
    {
        var socket = await GetSocket(requestUri).ConfigureAwait(false);
        var stream = new NetworkStream(socket, ownsSocket: true);

        if (requestUri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
        {
            var sslStream = new SslStream(stream, leaveInnerStreamOpen: false, userCertificateValidationCallback:
                validateCertificate ? null : (RemoteCertificateValidationCallback)((a, b, c, d) => true));

            await sslStream.AuthenticateAsClientAsync(requestUri.Host, clientCertificates: null,
                enabledSslProtocols: SslProtocols.None,
                checkCertificateRevocation: validateCertificate).ConfigureAwait(false);
            return sslStream;
        }
        else
        {
            return stream;
        }
    }

    public static async Task<Socket> GetSocket(Uri requestUri)
    {
        var tcs = new TaskCompletionSource<Socket>();

        var socketArgs = new SocketAsyncEventArgs();
        socketArgs.RemoteEndPoint = new DnsEndPoint(requestUri.DnsSafeHost, requestUri.Port);
        socketArgs.Completed += (s, e) => tcs.TrySetResult(e.ConnectSocket);

        // Must use static ConnectAsync(), since instance Connect() does not support DNS names on OSX/Linux.
        if (Socket.ConnectAsync(SocketType.Stream, ProtocolType.Tcp, socketArgs))
        {
            await tcs.Task.ConfigureAwait(false);
        }

        var socket = socketArgs.ConnectSocket;

        if (socket == null)
        {
            throw new SocketException((int)socketArgs.SocketError);
        }
        else
        {
            return socket;
        }
    }
}

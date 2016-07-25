// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Testing
{
    // Lightweight version of HttpClient implemented using Socket and SslStream
    public static class HttpClientSlim
    {
        public static Task<string> GetStringAsync(string requestUri, bool validateCertificate = true)
            => GetStringAsync(new Uri(requestUri), validateCertificate);

        public static async Task<string> GetStringAsync(Uri requestUri, bool validateCertificate = true)
        {
            using (var stream = await GetStream(requestUri, validateCertificate))
            {
                using (var writer = new StreamWriter(stream, Encoding.ASCII, bufferSize: 1024, leaveOpen: true))
                {
                    await writer.WriteAsync($"GET {requestUri.PathAndQuery} HTTP/1.0\r\n");
                    await writer.WriteAsync($"Host: {requestUri.Authority}\r\n");
                    await writer.WriteAsync("\r\n");
                }

                return await ReadResponse(stream);
            }
        }

        public static Task<string> PostAsync(string requestUri, HttpContent content, bool validateCertificate = true)
            => PostAsync(new Uri(requestUri), content, validateCertificate);

        public static async Task<string> PostAsync(Uri requestUri, HttpContent content, bool validateCertificate = true)
        {
            using (var stream = await GetStream(requestUri, validateCertificate))
            {
                using (var writer = new StreamWriter(stream, Encoding.ASCII, bufferSize: 1024, leaveOpen: true))
                {
                    await writer.WriteAsync($"POST {requestUri.PathAndQuery} HTTP/1.0\r\n");
                    await writer.WriteAsync($"Host: {requestUri.Authority}\r\n");
                    await writer.WriteAsync($"Content-Type: {content.Headers.ContentType}\r\n");
                    await writer.WriteAsync($"Content-Length: {content.Headers.ContentLength}\r\n");
                    await writer.WriteAsync("\r\n");
                }

                await content.CopyToAsync(stream);

                return await ReadResponse(stream);
            }
        }

        private static async Task<string> ReadResponse(Stream stream)
        {
            using (var reader = new StreamReader(stream, Encoding.ASCII, detectEncodingFromByteOrderMarks: true,
                bufferSize: 1024, leaveOpen: true))
            {
                var response = await reader.ReadToEndAsync();

                var status = GetStatus(response);
                new HttpResponseMessage(status).EnsureSuccessStatusCode();

                var body = response.Substring(response.IndexOf("\r\n\r\n") + 4);
                return body;
            }

        }

        private static HttpStatusCode GetStatus(string response)
        {
            var statusStart = response.IndexOf(' ') + 1;
            var statusEnd = response.IndexOf(' ', statusStart) - 1;
            var statusLength = statusEnd - statusStart + 1;
            return (HttpStatusCode)int.Parse(response.Substring(statusStart, statusLength));
        }

        private static async Task<Stream> GetStream(Uri requestUri, bool validateCertificate)
        {
            var socket = await GetSocket(requestUri);
            Stream stream = new NetworkStream(socket, ownsSocket: true);

            if (requestUri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
            {
                var sslStream = new SslStream(stream, leaveInnerStreamOpen: false, userCertificateValidationCallback:
                    validateCertificate ? null : (RemoteCertificateValidationCallback)((a, b, c, d) => true));
                await sslStream.AuthenticateAsClientAsync(requestUri.Host, clientCertificates: null,
                    enabledSslProtocols: SslProtocols.Tls11 | SslProtocols.Tls12,
                    checkCertificateRevocation: validateCertificate);
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
                await tcs.Task;
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
}

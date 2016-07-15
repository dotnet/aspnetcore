// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Server.Kestrel.FunctionalTests
{
    // Lightweight version of HttpClient implemented using Socket and SslStream
    public class HttpClientSlim
    {
        public bool ValidateCertificate { get; set; } = true;

        public Task<string> GetStringAsync(string requestUri) => GetStringAsync(new Uri(requestUri));

        public async Task<string> GetStringAsync(Uri requestUri)
        {
            using (var stream = await GetStream(requestUri))
            {
                using (var writer = new StreamWriter(stream, Encoding.ASCII, bufferSize: 1024, leaveOpen: true))
                {
                    await writer.WriteAsync($"GET {requestUri.PathAndQuery} HTTP/1.0\r\n");
                    await writer.WriteAsync($"Host: {requestUri.Authority}\r\n");
                    await writer.WriteAsync("\r\n");
                }

                using (var reader = new StreamReader(stream, Encoding.ASCII))
                {
                    var response = await reader.ReadToEndAsync();
                    var body = response.Substring(response.IndexOf("\r\n\r\n") + 4);
                    return body;
                }
            }
        }

        private async Task<Stream> GetStream(Uri requestUri)
        {
            var socket = await GetSocket(requestUri);
            Stream stream = new NetworkStream(socket, ownsSocket: true);

            if (requestUri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
            {
                var sslStream = new SslStream(stream, leaveInnerStreamOpen: false, userCertificateValidationCallback:
                    ValidateCertificate ? null : (RemoteCertificateValidationCallback)((a, b, c, d) => true));
                await sslStream.AuthenticateAsClientAsync(requestUri.Host, clientCertificates: null,
                    enabledSslProtocols: SslProtocols.Tls11 | SslProtocols.Tls12,
                    checkCertificateRevocation: ValidateCertificate);
                return sslStream;
            }
            else
            {
                return stream;
            }
        }

        private static async Task<Socket> GetSocket(Uri requestUri)
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

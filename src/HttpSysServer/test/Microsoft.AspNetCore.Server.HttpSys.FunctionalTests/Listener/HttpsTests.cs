// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;

namespace Microsoft.AspNetCore.Server.HttpSys.Listener
{
    public class HttpsTests
    {
        // Note these tests can't use dynamic ports or run concurrently because the ssl cert must be pre-registered with a specific port.
        private const string Address = "https://localhost:9090/";

        [ConditionalFact(Skip = "TODO: Add trait filtering support so these SSL tests don't get run on teamcity or the command line."), Trait("scheme", "https")]
        public async Task Https_200OK_Success()
        {
            using (var server = Utilities.CreateHttpsServer())
            {
                Task<string> responseTask = SendRequestAsync(Address);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout);
                context.Dispose();

                string response = await responseTask;
                Assert.Equal(string.Empty, response);
            }
        }

        [ConditionalFact(Skip = "TODO: Add trait filtering support so these SSL tests don't get run on teamcity or the command line."), Trait("scheme", "https")]
        public async Task Https_SendHelloWorld_Success()
        {
            using (var server = Utilities.CreateHttpsServer())
            {
                Task<string> responseTask = SendRequestAsync(Address);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout);
                byte[] body = Encoding.UTF8.GetBytes("Hello World");
                context.Response.ContentLength = body.Length;
                await context.Response.Body.WriteAsync(body, 0, body.Length);
                context.Dispose();

                string response = await responseTask;
                Assert.Equal("Hello World", response);
            }
        }

        [ConditionalFact(Skip = "TODO: Add trait filtering support so these SSL tests don't get run on teamcity or the command line."), Trait("scheme", "https")]
        public async Task Https_EchoHelloWorld_Success()
        {
            using (var server = Utilities.CreateHttpsServer())
            {
                Task<string> responseTask = SendRequestAsync(Address, "Hello World");

                var context = await server.AcceptAsync(Utilities.DefaultTimeout);
                string input = new StreamReader(context.Request.Body).ReadToEnd();
                Assert.Equal("Hello World", input);
                context.Response.ContentLength = 11;
                var writer = new StreamWriter(context.Response.Body);
                await writer.WriteAsync("Hello World");
                await writer.FlushAsync();

                string response = await responseTask;
                Assert.Equal("Hello World", response);
            }
        }

        [ConditionalFact(Skip = "TODO: Add trait filtering support so these SSL tests don't get run on teamcity or the command line."), Trait("scheme", "https")]
        public async Task Https_ClientCertNotSent_ClientCertNotPresent()
        {
            using (var server = Utilities.CreateHttpsServer())
            {
                Task<string> responseTask = SendRequestAsync(Address);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout);
                var cert = await context.Request.GetClientCertificateAsync();
                Assert.Null(cert);
                context.Dispose();

                string response = await responseTask;
                Assert.Equal(string.Empty, response);
            }
        }

        [ConditionalFact(Skip = "TODO: Add trait filtering support so these SSL tests don't get run on teamcity or the command line."), Trait("scheme", "https")]
        public async Task Https_ClientCertRequested_ClientCertPresent()
        {
            using (var server = Utilities.CreateHttpsServer())
            {
                X509Certificate2 clientCert = FindClientCert();
                Assert.NotNull(clientCert);
                Task<string> responseTask = SendRequestAsync(Address, clientCert);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout);
                var cert = await context.Request.GetClientCertificateAsync();
                Assert.NotNull(cert);
                context.Dispose();

                string response = await responseTask;
                Assert.Equal(string.Empty, response);
            }
        }

        private async Task<string> SendRequestAsync(string uri, 
            X509Certificate cert = null)
        {
            WinHttpHandler handler = new WinHttpHandler();
            handler.ServerCertificateValidationCallback = (a, b, c, d) => true;
            if (cert != null)
            {
                handler.ClientCertificates.Add(cert);
            }
            using (HttpClient client = new HttpClient(handler))
            {
                return await client.GetStringAsync(uri);
            }
        }

        private async Task<string> SendRequestAsync(string uri, string upload)
        {
            WinHttpHandler handler = new WinHttpHandler();
            handler.ServerCertificateValidationCallback = (a, b, c, d) => true;
            using (HttpClient client = new HttpClient(handler))
            {
                HttpResponseMessage response = await client.PostAsync(uri, new StringContent(upload));
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
        }

        private X509Certificate2 FindClientCert()
        {
            var store = new X509Store();
            store.Open(OpenFlags.ReadOnly);

            foreach (var cert in store.Certificates)
            {
                bool isClientAuth = false;
                bool isSmartCard = false;
                foreach (var extension in cert.Extensions)
                {
                    var eku = extension as X509EnhancedKeyUsageExtension;
                    if (eku != null)
                    {
                        foreach (var oid in eku.EnhancedKeyUsages)
                        {
                            if (oid.FriendlyName == "Client Authentication")
                            {
                                isClientAuth = true;
                            }
                            else if (oid.FriendlyName == "Smart Card Logon")
                            {
                                isSmartCard = true;
                                break;
                            }
                        }
                    }
                }

                if (isClientAuth && !isSmartCard)
                {
                    return cert;
                }
            }
            return null;
        }
    }
}
// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpSys.Internal;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Server.HttpSys
{
    public class HttpsTests
    {
        [ConditionalFact]
        public async Task Https_200OK_Success()
        {
            using (Utilities.CreateDynamicHttpsServer(out var address, httpContext =>
            {
                return Task.FromResult(0);
            }))
            {
                string response = await SendRequestAsync(address);
                Assert.Equal(string.Empty, response);
            }
        }

        [ConditionalFact]
        public async Task Https_SendHelloWorld_Success()
        {
            using (Utilities.CreateDynamicHttpsServer(out var address, httpContext =>
            {
                byte[] body = Encoding.UTF8.GetBytes("Hello World");
                httpContext.Response.ContentLength = body.Length;
                return httpContext.Response.Body.WriteAsync(body, 0, body.Length);
            }))
            {
                string response = await SendRequestAsync(address);
                Assert.Equal("Hello World", response);
            }
        }

        [ConditionalFact]
        public async Task Https_EchoHelloWorld_Success()
        {
            using (Utilities.CreateDynamicHttpsServer(out var address, async httpContext =>
            {
                var input = await new StreamReader(httpContext.Request.Body).ReadToEndAsync();
                Assert.Equal("Hello World", input);
                var body = Encoding.UTF8.GetBytes("Hello World");
                httpContext.Response.ContentLength = body.Length;
                await httpContext.Response.Body.WriteAsync(body, 0, body.Length);
            }))
            {
                string response = await SendRequestAsync(address, "Hello World");
                Assert.Equal("Hello World", response);
            }
        }

        [ConditionalFact]
        public async Task Https_ClientCertNotSent_ClientCertNotPresent()
        {
            using (Utilities.CreateDynamicHttpsServer(out var address, async httpContext =>
            {
                var tls = httpContext.Features.Get<ITlsConnectionFeature>();
                Assert.NotNull(tls);
                var cert = await tls.GetClientCertificateAsync(CancellationToken.None);
                Assert.Null(cert);
                Assert.Null(tls.ClientCertificate);
            }))
            {
                string response = await SendRequestAsync(address);
                Assert.Equal(string.Empty, response);
            }
        }

        [ConditionalFact(Skip = "Manual test only, client certs are not always available.")]
        public async Task Https_ClientCertRequested_ClientCertPresent()
        {
            using (Utilities.CreateDynamicHttpsServer(out var address, async httpContext =>
            {
                var tls = httpContext.Features.Get<ITlsConnectionFeature>();
                Assert.NotNull(tls);
                var cert = await tls.GetClientCertificateAsync(CancellationToken.None);
                Assert.NotNull(cert);
                Assert.NotNull(tls.ClientCertificate);
            }))
            {
                X509Certificate2 cert = FindClientCert();
                Assert.NotNull(cert);
                string response = await SendRequestAsync(address, cert);
                Assert.Equal(string.Empty, response);
            }
        }

        [ConditionalFact]
        [MaximumOSVersion(OperatingSystems.Windows, WindowsVersions.Win7)]
        public async Task Https_SkipsITlsHandshakeFeatureOnWin7()
        {
            using (Utilities.CreateDynamicHttpsServer(out var address, async httpContext =>
            {
                try
                {
                    var tlsFeature = httpContext.Features.Get<ITlsHandshakeFeature>();
                    Assert.Null(tlsFeature);
                }
                catch (Exception ex)
                {
                    await httpContext.Response.WriteAsync(ex.ToString());
                }
            }))
            {
                string response = await SendRequestAsync(address);
                Assert.Equal(string.Empty, response);
            }
        }

        [ConditionalFact]
        [MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win8)]
        public async Task Https_SetsITlsHandshakeFeature()
        {
            using (Utilities.CreateDynamicHttpsServer(out var address, async httpContext =>
            {
                try
                {
                    var tlsFeature = httpContext.Features.Get<ITlsHandshakeFeature>();
                    Assert.NotNull(tlsFeature);
                    Assert.True(tlsFeature.Protocol > SslProtocols.None, "Protocol");
                    Assert.True(Enum.IsDefined(typeof(SslProtocols), tlsFeature.Protocol), "Defined"); // Mapping is required, make sure it's current
                    Assert.True(tlsFeature.CipherAlgorithm > CipherAlgorithmType.Null, "Cipher");
                    Assert.True(tlsFeature.CipherStrength > 0, "CipherStrength");
                    Assert.True(tlsFeature.HashAlgorithm > HashAlgorithmType.None, "HashAlgorithm");
                    Assert.True(tlsFeature.HashStrength >= 0, "HashStrength"); // May be 0 for some algorithms
                    Assert.True(tlsFeature.KeyExchangeAlgorithm > ExchangeAlgorithmType.None, "KeyExchangeAlgorithm");
                    Assert.True(tlsFeature.KeyExchangeStrength > 0, "KeyExchangeStrength");
                }
                catch (Exception ex)
                {
                    await httpContext.Response.WriteAsync(ex.ToString());
                }
            }))
            {
                string response = await SendRequestAsync(address);
                Assert.Equal(string.Empty, response);
            }
        }

        [ConditionalFact]
        [MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win8)]
        public async Task Https_ITlsHandshakeFeature_MatchesIHttpSysExtensionInfoFeature()
        {
            using (Utilities.CreateDynamicHttpsServer(out var address, async httpContext =>
            {
                try
                {
                    var tlsFeature = httpContext.Features.Get<ITlsHandshakeFeature>();
                    var requestInfoFeature = httpContext.Features.Get<IHttpSysRequestInfoFeature>();
                    Assert.NotNull(tlsFeature);
                    Assert.NotNull(requestInfoFeature);
                    Assert.True(requestInfoFeature.RequestInfo.Count > 0);
                    var tlsInfo = requestInfoFeature.RequestInfo[(int)HttpApiTypes.HTTP_REQUEST_INFO_TYPE.HttpRequestInfoTypeSslProtocol];
                    HttpApiTypes.HTTP_SSL_PROTOCOL_INFO tlsCopy;
                    unsafe
                    {
                        using var handle = tlsInfo.Pin();
                        tlsCopy = Marshal.PtrToStructure<HttpApiTypes.HTTP_SSL_PROTOCOL_INFO>((IntPtr)handle.Pointer);
                    }

                    // Assert.Equal(tlsFeature.Protocol, tlsCopy.Protocol); // These don't directly match because the native and managed enums use different values.
                    Assert.Equal(tlsFeature.CipherAlgorithm, tlsCopy.CipherType);
                    Assert.Equal(tlsFeature.CipherStrength, (int)tlsCopy.CipherStrength);
                    Assert.Equal(tlsFeature.HashAlgorithm, tlsCopy.HashType);
                    Assert.Equal(tlsFeature.HashStrength, (int)tlsCopy.HashStrength);
                    Assert.Equal(tlsFeature.KeyExchangeAlgorithm, tlsCopy.KeyExchangeType);
                    Assert.Equal(tlsFeature.KeyExchangeStrength, (int)tlsCopy.KeyExchangeStrength);
                }
                catch (Exception ex)
                {
                    await httpContext.Response.WriteAsync(ex.ToString());
                }
            }))
            {
                string response = await SendRequestAsync(address);
                Assert.Equal(string.Empty, response);
            }
        }

        private async Task<string> SendRequestAsync(string uri,
            X509Certificate cert = null)
        {
            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            if (cert != null)
            {
                handler.ClientCertificates.Add(cert);
            }
            using HttpClient client = new HttpClient(handler);
            return await client.GetStringAsync(uri);
        }

        private async Task<string> SendRequestAsync(string uri, string upload)
        {
            var handler = new WinHttpHandler();
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

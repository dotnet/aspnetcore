// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpSys.Internal;
using Microsoft.AspNetCore.InternalTesting;
using Windows.Win32.Networking.HttpServer;
using Xunit;

namespace Microsoft.AspNetCore.Server.HttpSys;

public class HttpsTests : LoggedTest
{
    private static readonly X509Certificate2 _x509Certificate2 = TestResources.GetTestCertificate("eku.client.pfx");

    [ConditionalFact]
    public async Task Https_200OK_Success()
    {
        using (Utilities.CreateDynamicHttpsServer(out var address, httpContext =>
        {
            return Task.FromResult(0);
        }, LoggerFactory))
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
        }, LoggerFactory))
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
        }, LoggerFactory))
        {
            string response = await SendRequestAsync(address, "Hello World");
            Assert.Equal("Hello World", response);
        }
    }

    [ConditionalTheory]
    [InlineData(ClientCertificateMethod.NoCertificate)]
    [InlineData(ClientCertificateMethod.AllowCertificate)]
    [InlineData(ClientCertificateMethod.AllowRenegotation)]
    public async Task Https_ClientCertNotSent_ClientCertNotPresent(ClientCertificateMethod clientCertificateMethod)
    {
        using (Utilities.CreateDynamicHttpsServer("", out var root, out var address, options =>
        {
            options.ClientCertificateMethod = clientCertificateMethod;
        },
        async httpContext =>
        {
            var tls = httpContext.Features.Get<ITlsConnectionFeature>();
            Assert.NotNull(tls);
            Assert.Null(tls.ClientCertificate);
            var cert = await tls.GetClientCertificateAsync(CancellationToken.None);
            Assert.Null(cert);
            Assert.Null(tls.ClientCertificate);
        }))
        {
            var response = await SendRequestAsync(address);
            Assert.Equal(string.Empty, response);
        }
    }

    [ConditionalTheory]
    [InlineData(ClientCertificateMethod.NoCertificate)]
    [InlineData(ClientCertificateMethod.AllowCertificate)]
    [InlineData(ClientCertificateMethod.AllowRenegotation)]
    public async Task Https_ClientCertRequested_ClientCertPresent(ClientCertificateMethod clientCertificateMethod)
    {
        using (Utilities.CreateDynamicHttpsServer("", out var root, out var address, options =>
        {
            options.ClientCertificateMethod = clientCertificateMethod;
        },
        async httpContext =>
        {
            var tls = httpContext.Features.Get<ITlsConnectionFeature>();
            Assert.NotNull(tls);
            Assert.Null(tls.ClientCertificate);
            var cert = await tls.GetClientCertificateAsync(CancellationToken.None);
            if (clientCertificateMethod == ClientCertificateMethod.AllowRenegotation)
            {
                Assert.NotNull(cert);
                Assert.NotNull(tls.ClientCertificate);
            }
            else
            {
                Assert.Null(cert);
                Assert.Null(tls.ClientCertificate);
            }
        }))
        {
            Assert.NotNull(_x509Certificate2);
            var response = await SendRequestAsync(address, _x509Certificate2);
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
        }, LoggerFactory))
        {
            string response = await SendRequestAsync(address);
            Assert.Equal(string.Empty, response);
        }
    }

    [ConditionalFact]
    [MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win8)]
    public async Task Https_SetsITlsHandshakeFeature()
    {
        using (Utilities.CreateDynamicHttpsServer(out var address, httpContext =>
        {
            var tlsFeature = httpContext.Features.Get<ITlsHandshakeFeature>();
            Assert.NotNull(tlsFeature);
            return httpContext.Response.WriteAsJsonAsync(tlsFeature);
        }, LoggerFactory))
        {
            string response = await SendRequestAsync(address);
            var result = System.Text.Json.JsonDocument.Parse(response).RootElement;

            var protocol = (SslProtocols)result.GetProperty("protocol").GetInt32();
            Assert.True(protocol > SslProtocols.None, "Protocol: " + protocol);
            Assert.True(Enum.IsDefined(typeof(SslProtocols), protocol), "Defined: " + protocol); // Mapping is required, make sure it's current

#pragma warning disable SYSLIB0058 // Type or member is obsolete
            var cipherAlgorithm = (CipherAlgorithmType)result.GetProperty("cipherAlgorithm").GetInt32();
            Assert.True(cipherAlgorithm > CipherAlgorithmType.Null, "Cipher: " + cipherAlgorithm);
#pragma warning restore SYSLIB0058 // Type or member is obsolete

            var cipherStrength = result.GetProperty("cipherStrength").GetInt32();
            Assert.True(cipherStrength > 0, "CipherStrength: " + cipherStrength);

#pragma warning disable SYSLIB0058 // Type or member is obsolete
            var hashAlgorithm = (HashAlgorithmType)result.GetProperty("hashAlgorithm").GetInt32();
            Assert.True(hashAlgorithm >= HashAlgorithmType.None, "HashAlgorithm: " + hashAlgorithm);
#pragma warning restore SYSLIB0058 // Type or member is obsolete

            var hashStrength = result.GetProperty("hashStrength").GetInt32();
            Assert.True(hashStrength >= 0, "HashStrength: " + hashStrength); // May be 0 for some algorithms

#pragma warning disable SYSLIB0058 // Type or member is obsolete
            var keyExchangeAlgorithm = (ExchangeAlgorithmType)result.GetProperty("keyExchangeAlgorithm").GetInt32();
            Assert.True(keyExchangeAlgorithm >= ExchangeAlgorithmType.None, "KeyExchangeAlgorithm: " + keyExchangeAlgorithm);
#pragma warning restore SYSLIB0058 // Type or member is obsolete

            var keyExchangeStrength = result.GetProperty("keyExchangeStrength").GetInt32();
            Assert.True(keyExchangeStrength >= 0, "KeyExchangeStrength: " + keyExchangeStrength);

            if (Environment.OSVersion.Version > new Version(10, 0, 19043, 0))
            {
                var hostName = result.GetProperty("hostName").ToString();
                Assert.Equal("localhost", hostName);
            }
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
                var tlsInfo = requestInfoFeature.RequestInfo[(int)HTTP_REQUEST_INFO_TYPE.HttpRequestInfoTypeSslProtocol];
                HTTP_SSL_PROTOCOL_INFO tlsCopy;
                unsafe
                {
                    using var handle = tlsInfo.Pin();
                    tlsCopy = Marshal.PtrToStructure<HTTP_SSL_PROTOCOL_INFO>((IntPtr)handle.Pointer);
                }

                // Assert.Equal(tlsFeature.Protocol, tlsCopy.Protocol); // These don't directly match because the native and managed enums use different values.
#pragma warning disable SYSLIB0058 // Type or member is obsolete
                Assert.Equal((uint)tlsFeature.CipherAlgorithm, tlsCopy.CipherType);
                Assert.Equal(tlsFeature.CipherStrength, (int)tlsCopy.CipherStrength);
                Assert.Equal((uint)tlsFeature.HashAlgorithm, tlsCopy.HashType);
                Assert.Equal(tlsFeature.HashStrength, (int)tlsCopy.HashStrength);
                Assert.Equal((uint)tlsFeature.KeyExchangeAlgorithm, tlsCopy.KeyExchangeType);
                Assert.Equal(tlsFeature.KeyExchangeStrength, (int)tlsCopy.KeyExchangeStrength);
#pragma warning restore SYSLIB0058 // Type or member is obsolete
            }
            catch (Exception ex)
            {
                await httpContext.Response.WriteAsync(ex.ToString());
            }
        }, LoggerFactory))
        {
            string response = await SendRequestAsync(address);
            Assert.Equal(string.Empty, response);
        }
    }

    [ConditionalFact]
    [MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win10_20H2)]
    public async Task Https_SetsIHttpSysRequestTimingFeature()
    {
        using (Utilities.CreateDynamicHttpsServer(out var address, async httpContext =>
        {
            try
            {
                var requestTimingFeature = httpContext.Features.Get<IHttpSysRequestTimingFeature>();
                Assert.NotNull(requestTimingFeature);
                Assert.True(requestTimingFeature.Timestamps.Length > (int)HttpSysRequestTimingType.Http3HeaderDecodeEnd);
                Assert.True(requestTimingFeature.TryGetTimestamp(HttpSysRequestTimingType.RequestHeaderParseStart, out var headerStart));
                Assert.True(requestTimingFeature.TryGetTimestamp(HttpSysRequestTimingType.RequestHeaderParseEnd, out var headerEnd));
                Assert.True(requestTimingFeature.TryGetElapsedTime(HttpSysRequestTimingType.RequestHeaderParseStart, HttpSysRequestTimingType.RequestHeaderParseEnd, out var elapsed));
                Assert.Equal(Stopwatch.GetElapsedTime(headerStart, headerEnd), elapsed);
                Assert.False(requestTimingFeature.TryGetTimestamp(HttpSysRequestTimingType.Http3StreamStart, out var streamStart));
                Assert.False(requestTimingFeature.TryGetTimestamp((HttpSysRequestTimingType)int.MaxValue, out var invalid));
                Assert.False(requestTimingFeature.TryGetElapsedTime(HttpSysRequestTimingType.Http3StreamStart, HttpSysRequestTimingType.RequestHeaderParseStart, out elapsed));
            }
            catch (Exception ex)
            {
                await httpContext.Response.WriteAsync(ex.ToString());
            }
        }, LoggerFactory))
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

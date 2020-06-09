// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNetCore.HttpOverrides
{
    public class CertificateForwardingTests
    {
        [Fact]
        public void VerifySettingNullHeaderOptionThrows()
        {
            var services = new ServiceCollection()
                .AddOptions()
                .AddCertificateForwarding(o => o.CertificateHeader = null);
            var options = services.BuildServiceProvider().GetRequiredService<IOptions<CertificateForwardingOptions>>();
            Assert.Throws<OptionsValidationException>(() => options.Value);
        }

        [Fact]
        public void VerifySettingEmptyHeaderOptionThrows()
        {
            var services = new ServiceCollection()
                .AddOptions()
                .AddCertificateForwarding(o => o.CertificateHeader = "");
            var options = services.BuildServiceProvider().GetRequiredService<IOptions<CertificateForwardingOptions>>();
            Assert.Throws<OptionsValidationException>(() => options.Value);
        }

        [Fact]
        public async Task VerifyHeaderIsUsedIfNoCertificateAlreadySet()
        {
            var builder = new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddCertificateForwarding(options => { });
                })
                .Configure(app =>
                {
                    app.Use(async (context, next) =>
                    {
                        Assert.Null(context.Connection.ClientCertificate);
                        await next();
                    });
                    app.UseCertificateForwarding();
                    app.Use(async (context, next) =>
                    {
                        Assert.Equal(context.Connection.ClientCertificate, Certificates.SelfSignedValidWithNoEku);
                        await next();
                    });
                });
            var server = new TestServer(builder);

            var context = await server.SendAsync(c =>
            {
                c.Request.Headers["X-Client-Cert"] = Convert.ToBase64String(Certificates.SelfSignedValidWithNoEku.RawData);
            });
        }

        [Fact]
        public async Task VerifyHeaderOverridesCertificateEvenAlreadySet()
        {
            var builder = new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddCertificateForwarding(options => { });
                })
                .Configure(app =>
                {
                    app.Use(async (context, next) =>
                    {
                        Assert.Null(context.Connection.ClientCertificate);
                        context.Connection.ClientCertificate = Certificates.SelfSignedNotYetValid;
                        await next();
                    });
                    app.UseCertificateForwarding();
                    app.Use(async (context, next) =>
                    {
                        Assert.Equal(context.Connection.ClientCertificate, Certificates.SelfSignedValidWithNoEku);
                        await next();
                    });
                });
            var server = new TestServer(builder);

            var context = await server.SendAsync(c =>
            {
                c.Request.Headers["X-Client-Cert"] = Convert.ToBase64String(Certificates.SelfSignedValidWithNoEku.RawData);
            });
        }

        [Fact]
        public async Task VerifySettingTheAzureHeaderOnTheForwarderOptionsWorks()
        {
            var builder = new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddCertificateForwarding(options => options.CertificateHeader = "X-ARR-ClientCert");
                })
                .Configure(app =>
                {
                    app.Use(async (context, next) =>
                    {
                        Assert.Null(context.Connection.ClientCertificate);
                        await next();
                    });
                    app.UseCertificateForwarding();
                    app.Use(async (context, next) =>
                    {
                        Assert.Equal(context.Connection.ClientCertificate, Certificates.SelfSignedValidWithNoEku);
                        await next();
                    });
                });
            var server = new TestServer(builder);

            var context = await server.SendAsync(c =>
            {
                c.Request.Headers["X-ARR-ClientCert"] = Convert.ToBase64String(Certificates.SelfSignedValidWithNoEku.RawData);
            });
        }

        [Fact]
        public async Task VerifyACustomHeaderFailsIfTheHeaderIsNotPresent()
        {
            var builder = new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddCertificateForwarding(options => options.CertificateHeader = "some-random-header");
                })
                .Configure(app =>
                {
                    app.Use(async (context, next) =>
                    {
                        Assert.Null(context.Connection.ClientCertificate);
                        await next();
                    });
                    app.UseCertificateForwarding();
                    app.Use(async (context, next) =>
                    {
                        Assert.Null(context.Connection.ClientCertificate);
                        await next();
                    });
                });
            var server = new TestServer(builder);

            var context = await server.SendAsync(c =>
            {
                c.Request.Headers["not-the-right-header"] = Convert.ToBase64String(Certificates.SelfSignedValidWithNoEku.RawData);
            });
        }

        [Fact]
        public async Task VerifyArrHeaderEncodedCertFailsOnBadEncoding()
        {
            var builder = new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddCertificateForwarding(options => { });
                })
                .Configure(app =>
                {
                    app.Use(async (context, next) =>
                    {
                        Assert.Null(context.Connection.ClientCertificate);
                        await next();
                    });
                    app.UseCertificateForwarding();
                    app.Use(async (context, next) =>
                    {
                        Assert.Null(context.Connection.ClientCertificate);
                        await next();
                    });
                });
            var server = new TestServer(builder);

            var context = await server.SendAsync(c =>
            {
                c.Request.Headers["X-Client-Cert"] = "OOPS" + Convert.ToBase64String(Certificates.SelfSignedValidWithNoEku.RawData);
            });
        }

        private static class Certificates
        {
            public static X509Certificate2 SelfSignedValidWithClientEku { get; private set; } =
                new X509Certificate2(GetFullyQualifiedFilePath("validSelfSignedClientEkuCertificate.cer"));

            public static X509Certificate2 SelfSignedValidWithNoEku { get; private set; } =
                new X509Certificate2(GetFullyQualifiedFilePath("validSelfSignedNoEkuCertificate.cer"));

            public static X509Certificate2 SelfSignedValidWithServerEku { get; private set; } =
                new X509Certificate2(GetFullyQualifiedFilePath("validSelfSignedServerEkuCertificate.cer"));

            public static X509Certificate2 SelfSignedNotYetValid { get; private set; } =
                new X509Certificate2(GetFullyQualifiedFilePath("selfSignedNoEkuCertificateNotValidYet.cer"));

            public static X509Certificate2 SelfSignedExpired { get; private set; } =
                new X509Certificate2(GetFullyQualifiedFilePath("selfSignedNoEkuCertificateExpired.cer"));

            private static string GetFullyQualifiedFilePath(string filename)
            {
                var filePath = Path.Combine(AppContext.BaseDirectory, filename);
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException(filePath);
                }
                return filePath;
            }
        }

    }
}

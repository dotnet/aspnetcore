// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Tests
{
    public class KestrelConfigurationBuilderTests
    {
        private KestrelServerOptions CreateServerOptions()
        {
            var serverOptions = new KestrelServerOptions();
            serverOptions.ApplicationServices = new ServiceCollection()
                .AddLogging()
                .AddSingleton<IHostingEnvironment>(new HostingEnvironment { ApplicationName = "TestApplication" })
                .BuildServiceProvider();
            return serverOptions;
        }

        [Fact]
        public void ConfigureNamedEndpoint_OnlyRunForMatchingConfig()
        {
            var found = false;
            var serverOptions = CreateServerOptions();
            var config = new ConfigurationBuilder().AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string>("Endpoints:Found:Url", "http://*:5001"),
            }).Build();
            serverOptions.Configure(config)
                .Endpoint("Found", endpointOptions => found = true)
                .Endpoint("NotFound", endpointOptions => throw new NotImplementedException())
                .Load();

            Assert.Single(serverOptions.ListenOptions);
            Assert.Equal(5001, serverOptions.ListenOptions[0].IPEndPoint.Port);

            Assert.True(found);
        }

        [Fact]
        public void ConfigureEndpoint_OnlyRunWhenBuildIsCalled()
        {
            var run = false;
            var serverOptions = CreateServerOptions();
            serverOptions.Configure()
                .LocalhostEndpoint(5001, endpointOptions => run = true);

            Assert.Empty(serverOptions.ListenOptions);

            serverOptions.ConfigurationLoader.Load();

            Assert.Single(serverOptions.ListenOptions);
            Assert.Equal(5001, serverOptions.ListenOptions[0].IPEndPoint.Port);

            Assert.True(run);
        }

        [Fact]
        public void CallBuildTwice_OnlyRunsOnce()
        {
            var serverOptions = CreateServerOptions();
            var builder = serverOptions.Configure()
                .LocalhostEndpoint(5001);

            Assert.Empty(serverOptions.ListenOptions);
            Assert.Equal(builder, serverOptions.ConfigurationLoader);

            builder.Load();

            Assert.Single(serverOptions.ListenOptions);
            Assert.Equal(5001, serverOptions.ListenOptions[0].IPEndPoint.Port);
            Assert.NotNull(serverOptions.ConfigurationLoader);

            builder.Load();

            Assert.Single(serverOptions.ListenOptions);
            Assert.Equal(5001, serverOptions.ListenOptions[0].IPEndPoint.Port);
            Assert.NotNull(serverOptions.ConfigurationLoader);
        }

        [Fact]
        public void Configure_IsReplaceable()
        {
            var run1 = false;
            var serverOptions = CreateServerOptions();
            var config1 = new ConfigurationBuilder().AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string>("Endpoints:End1:Url", "http://*:5001"),
            }).Build();
            serverOptions.Configure(config1)
                .LocalhostEndpoint(5001, endpointOptions => run1 = true);

            Assert.Empty(serverOptions.ListenOptions);
            Assert.False(run1);

            var run2 = false;
            var config2 = new ConfigurationBuilder().AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string>("Endpoints:End2:Url", "http://*:5002"),
            }).Build();
            serverOptions.Configure(config2)
                .LocalhostEndpoint(5003, endpointOptions => run2 = true);

            serverOptions.ConfigurationLoader.Load();

            Assert.Equal(2, serverOptions.ListenOptions.Count);
            Assert.Equal(5002, serverOptions.ListenOptions[0].IPEndPoint.Port);
            Assert.Equal(5003, serverOptions.ListenOptions[1].IPEndPoint.Port);

            Assert.False(run1);
            Assert.True(run2);
        }

        [Fact]
        public void ConfigureDefaultsAppliesToNewConfigureEndpoints()
        {
            var serverOptions = CreateServerOptions();

            serverOptions.ConfigureEndpointDefaults(opt =>
            {
                opt.NoDelay = false;
                opt.Protocols = HttpProtocols.Http2;
            });

            serverOptions.ConfigureHttpsDefaults(opt =>
            {
                opt.ServerCertificate = TestResources.GetTestCertificate();
                opt.ClientCertificateMode = ClientCertificateMode.RequireCertificate;
            });

            var ran1 = false;
            var ran2 = false;
            var config = new ConfigurationBuilder().AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string>("Endpoints:End1:Url", "https://*:5001"),
            }).Build();
            serverOptions.Configure(config)
                .Endpoint("End1", opt =>
                {
                    ran1 = true;
                    Assert.True(opt.IsHttps);
                    Assert.NotNull(opt.HttpsOptions.ServerCertificate);
                    Assert.Equal(ClientCertificateMode.RequireCertificate, opt.HttpsOptions.ClientCertificateMode);
                    Assert.False(opt.ListenOptions.NoDelay);
                    Assert.Equal(HttpProtocols.Http2, opt.ListenOptions.Protocols);
                })
                .LocalhostEndpoint(5002, opt =>
                {
                    ran2 = true;
                    Assert.False(opt.NoDelay);
                    Assert.Equal(HttpProtocols.Http2, opt.Protocols);
                })
                .Load();

            Assert.True(ran1);
            Assert.True(ran2);

            Assert.NotNull(serverOptions.ListenOptions[0].ConnectionAdapters.Where(adapter => adapter.IsHttps).SingleOrDefault());
            Assert.Null(serverOptions.ListenOptions[1].ConnectionAdapters.Where(adapter => adapter.IsHttps).SingleOrDefault());
        }

        [Fact]
        public void ConfigureEndpointDefaultCanEnableHttps()
        {
            var serverOptions = CreateServerOptions();

            serverOptions.ConfigureEndpointDefaults(opt =>
            {
                opt.NoDelay = false;
                opt.UseHttps(TestResources.GetTestCertificate());
            });

            serverOptions.ConfigureHttpsDefaults(opt =>
            {
                opt.ClientCertificateMode = ClientCertificateMode.RequireCertificate;
            });

            var ran1 = false;
            var ran2 = false;
            var config = new ConfigurationBuilder().AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string>("Endpoints:End1:Url", "https://*:5001"),
            }).Build();
            serverOptions.Configure(config)
                .Endpoint("End1", opt =>
                {
                    ran1 = true;
                    Assert.True(opt.IsHttps);
                    Assert.Equal(ClientCertificateMode.RequireCertificate, opt.HttpsOptions.ClientCertificateMode);
                    Assert.False(opt.ListenOptions.NoDelay);
                })
                .LocalhostEndpoint(5002, opt =>
                {
                    ran2 = true;
                    Assert.False(opt.NoDelay);
                })
                .Load();

            Assert.True(ran1);
            Assert.True(ran2);

            // You only get Https once per endpoint.
            Assert.NotNull(serverOptions.ListenOptions[0].ConnectionAdapters.Where(adapter => adapter.IsHttps).SingleOrDefault());
            Assert.NotNull(serverOptions.ListenOptions[1].ConnectionAdapters.Where(adapter => adapter.IsHttps).SingleOrDefault());
        }

        [Fact]
        public void ConfigureEndpointDevelopmentCertificateGetsLoadedWhenPresent()
        {
            try
            {
                var serverOptions = CreateServerOptions();
                var certificate = new X509Certificate2(TestResources.GetCertPath("aspnetdevcert.pfx"), "aspnetdevcert", X509KeyStorageFlags.Exportable);
                var bytes = certificate.Export(X509ContentType.Pkcs12, "1234");
                var path = GetCertificatePath();
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllBytes(path, bytes);

                var ran1 = false;
                var config = new ConfigurationBuilder().AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string>("Endpoints:End1:Url", "https://*:5001"),
                    new KeyValuePair<string, string>("Certificates:Development:Password", "1234"),
                }).Build();

                serverOptions
                    .Configure(config)
                    .Endpoint("End1", opt =>
                    {
                        ran1 = true;
                        Assert.True(opt.IsHttps);
                        Assert.Equal(opt.HttpsOptions.ServerCertificate.SerialNumber, certificate.SerialNumber);
                    }).Load();

                Assert.True(ran1);
                Assert.NotNull(serverOptions.DefaultCertificate);
            }
            finally
            {
                if (File.Exists(GetCertificatePath()))
                {
                    File.Delete(GetCertificatePath());
                }
            }
        }

        [Fact]
        public void ConfigureEndpointDevelopmentCertificateGetsIgnoredIfPasswordIsNotCorrect()
        {
            try
            {
                var serverOptions = CreateServerOptions();
                var certificate = new X509Certificate2(TestResources.GetCertPath("aspnetdevcert.pfx"), "aspnetdevcert", X509KeyStorageFlags.Exportable);
                var bytes = certificate.Export(X509ContentType.Pkcs12, "1234");
                var path = GetCertificatePath();
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllBytes(path, bytes);

                var config = new ConfigurationBuilder().AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string>("Certificates:Development:Password", "12341234"),
                }).Build();

                serverOptions
                    .Configure(config)
                    .Load();

                Assert.Null(serverOptions.DefaultCertificate);
            }
            finally
            {
                if (File.Exists(GetCertificatePath()))
                {
                    File.Delete(GetCertificatePath());
                }
            }
        }

        [Fact]
        public void ConfigureEndpointDevelopmentCertificateGetsIgnoredIfPfxFileDoesNotExist()
        {
            try
            {
                var serverOptions = CreateServerOptions();
                if (File.Exists(GetCertificatePath()))
                {
                    File.Delete(GetCertificatePath());
                }

                var config = new ConfigurationBuilder().AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string>("Certificates:Development:Password", "12341234")
                }).Build();

                serverOptions
                    .Configure(config)
                    .Load();

                Assert.Null(serverOptions.DefaultCertificate);
            }
            finally
            {
                if (File.Exists(GetCertificatePath()))
                {
                    File.Delete(GetCertificatePath());
                }
            }
        }

        [Theory]
        [InlineData("http1", HttpProtocols.Http1)]
        [InlineData("http2", HttpProtocols.Http2)]
        [InlineData("http1AndHttp2", HttpProtocols.Http1AndHttp2)]
        public void DefaultConfigSectionCanSetProtocols(string input, HttpProtocols expected)
        {
            var serverOptions = CreateServerOptions();
            var ranDefault = false;
            serverOptions.ConfigureEndpointDefaults(opt =>
            {
                Assert.Equal(expected, opt.Protocols);
                ranDefault = true;
            });

            serverOptions.ConfigureHttpsDefaults(opt =>
            {
                opt.ServerCertificate = TestResources.GetTestCertificate();
                opt.ClientCertificateMode = ClientCertificateMode.RequireCertificate;
            });

            var ran1 = false;
            var ran2 = false;
            var ran3 = false;
            var config = new ConfigurationBuilder().AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string>("EndpointDefaults:Protocols", input),
                new KeyValuePair<string, string>("Endpoints:End1:Url", "https://*:5001"),
            }).Build();
            serverOptions.Configure(config)
                .Endpoint("End1", opt =>
                {
                    Assert.True(opt.IsHttps);
                    Assert.NotNull(opt.HttpsOptions.ServerCertificate);
                    Assert.Equal(ClientCertificateMode.RequireCertificate, opt.HttpsOptions.ClientCertificateMode);
                    Assert.Equal(expected, opt.ListenOptions.Protocols);
                    ran1 = true;
                })
                .LocalhostEndpoint(5002, opt =>
                {
                    Assert.Equal(expected, opt.Protocols);
                    ran2 = true;
                })
                .Load();
            serverOptions.ListenAnyIP(0, opt =>
            {
                Assert.Equal(expected, opt.Protocols);
                ran3 = true;
            });

            Assert.True(ranDefault);
            Assert.True(ran1);
            Assert.True(ran2);
            Assert.True(ran3);
        }

        [Theory]
        [InlineData("http1", HttpProtocols.Http1)]
        [InlineData("http2", HttpProtocols.Http2)]
        [InlineData("http1AndHttp2", HttpProtocols.Http1AndHttp2)]
        public void EndpointConfigSectionCanSetProtocols(string input, HttpProtocols expected)
        {
            var serverOptions = CreateServerOptions();
            var ranDefault = false;
            serverOptions.ConfigureEndpointDefaults(opt =>
            {
                // Kestrel default.
                Assert.Equal(HttpProtocols.Http1AndHttp2, opt.Protocols);
                ranDefault = true;
            });

            serverOptions.ConfigureHttpsDefaults(opt =>
            {
                opt.ServerCertificate = TestResources.GetTestCertificate();
                opt.ClientCertificateMode = ClientCertificateMode.RequireCertificate;
            });

            var ran1 = false;
            var ran2 = false;
            var ran3 = false;
            var config = new ConfigurationBuilder().AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string>("Endpoints:End1:Protocols", input),
                new KeyValuePair<string, string>("Endpoints:End1:Url", "https://*:5001"),
            }).Build();
            serverOptions.Configure(config)
                .Endpoint("End1", opt =>
                {
                    Assert.True(opt.IsHttps);
                    Assert.NotNull(opt.HttpsOptions.ServerCertificate);
                    Assert.Equal(ClientCertificateMode.RequireCertificate, opt.HttpsOptions.ClientCertificateMode);
                    Assert.Equal(expected, opt.ListenOptions.Protocols);
                    ran1 = true;
                })
                .LocalhostEndpoint(5002, opt =>
                {
                    // Kestrel default.
                    Assert.Equal(HttpProtocols.Http1AndHttp2, opt.Protocols);
                    ran2 = true;
                })
                .Load();
            serverOptions.ListenAnyIP(0, opt =>
            {
                // Kestrel default.
                Assert.Equal(HttpProtocols.Http1AndHttp2, opt.Protocols);
                ran3 = true;
            });

            Assert.True(ranDefault);
            Assert.True(ran1);
            Assert.True(ran2);
            Assert.True(ran3);
        }

        private static string GetCertificatePath()
        {
            var appData = Environment.GetEnvironmentVariable("APPDATA");
            var home = Environment.GetEnvironmentVariable("HOME");
            var basePath = appData != null ? Path.Combine(appData, "ASP.NET", "https") : null;
            basePath = basePath ?? (home != null ? Path.Combine(home, ".aspnet", "https") : null);
            return Path.Combine(basePath, $"TestApplication.pfx");
        }
    }
}

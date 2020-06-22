// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Tests
{
    public class KestrelConfigurationLoaderTests
    {
        private KestrelServerOptions CreateServerOptions()
        {
            var serverOptions = new KestrelServerOptions();
            var env = new MockHostingEnvironment { ApplicationName = "TestApplication" };
            serverOptions.ApplicationServices = new ServiceCollection()
                .AddLogging()
                .AddSingleton<IHostEnvironment>(env)
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
            Assert.Equal(5001, serverOptions.ConfigurationBackedListenOptions[0].IPEndPoint.Port);

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
            Assert.Equal(5001, serverOptions.CodeBackedListenOptions[0].IPEndPoint.Port);

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
            Assert.Equal(5001, serverOptions.CodeBackedListenOptions[0].IPEndPoint.Port);
            Assert.NotNull(serverOptions.ConfigurationLoader);

            builder.Load();

            Assert.Single(serverOptions.ListenOptions);
            Assert.Equal(5001, serverOptions.CodeBackedListenOptions[0].IPEndPoint.Port);
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

            Assert.Equal(2, serverOptions.ListenOptions.Count());
            Assert.Equal(5002, serverOptions.ConfigurationBackedListenOptions[0].IPEndPoint.Port);
            Assert.Equal(5003, serverOptions.CodeBackedListenOptions[0].IPEndPoint.Port);

            Assert.False(run1);
            Assert.True(run2);
        }

        [Fact]
        public void ConfigureDefaultsAppliesToNewConfigureEndpoints()
        {
            var serverOptions = CreateServerOptions();

            serverOptions.ConfigureEndpointDefaults(opt =>
            {
                opt.Protocols = HttpProtocols.Http1;
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
                    Assert.Equal(HttpProtocols.Http1, opt.ListenOptions.Protocols);
                })
                .LocalhostEndpoint(5002, opt =>
                {
                    ran2 = true;
                    Assert.Equal(HttpProtocols.Http1, opt.Protocols);
                })
                .Load();

            Assert.True(ran1);
            Assert.True(ran2);

            Assert.True(serverOptions.ConfigurationBackedListenOptions[0].IsTls);
            Assert.False(serverOptions.CodeBackedListenOptions[0].IsTls);
        }

        [Fact]
        public void ConfigureEndpointDefaultCanEnableHttps()
        {
            var serverOptions = CreateServerOptions();

            serverOptions.ConfigureEndpointDefaults(opt =>
            {
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
                })
                .LocalhostEndpoint(5002, opt =>
                {
                    ran2 = true;
                })
                .Load();

            Assert.True(ran1);
            Assert.True(ran2);

            // You only get Https once per endpoint.
            Assert.True(serverOptions.ConfigurationBackedListenOptions[0].IsTls);
            Assert.True(serverOptions.CodeBackedListenOptions[0].IsTls);
        }

        [Fact]
        public void ConfigureEndpointDevelopmentCertificateGetsLoadedWhenPresent()
        {
            try
            {
                var serverOptions = CreateServerOptions();
                var certificate = new X509Certificate2(TestResources.GetCertPath("aspnetdevcert.pfx"), "testPassword", X509KeyStorageFlags.Exportable);
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
                var certificate = new X509Certificate2(TestResources.GetCertPath("aspnetdevcert.pfx"), "testPassword", X509KeyStorageFlags.Exportable);
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

        [ConditionalTheory]
        [InlineData("http1", HttpProtocols.Http1)]
        // [InlineData("http2", HttpProtocols.Http2)] // Not supported due to missing ALPN support. https://github.com/dotnet/corefx/issues/33016
        [InlineData("http1AndHttp2", HttpProtocols.Http1AndHttp2)] // Gracefully falls back to HTTP/1
        [OSSkipCondition(OperatingSystems.Linux)]
        [MaximumOSVersion(OperatingSystems.Windows, WindowsVersions.Win7)]
        public void DefaultConfigSectionCanSetProtocols_MacAndWin7(string input, HttpProtocols expected)
            => DefaultConfigSectionCanSetProtocols(input, expected);

        [ConditionalTheory]
        [InlineData("http1", HttpProtocols.Http1)]
        [InlineData("http2", HttpProtocols.Http2)]
        [InlineData("http1AndHttp2", HttpProtocols.Http1AndHttp2)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win81)]
        public void DefaultConfigSectionCanSetProtocols_NonMacAndWin7(string input, HttpProtocols expected)
            => DefaultConfigSectionCanSetProtocols(input, expected);

        private void DefaultConfigSectionCanSetProtocols(string input, HttpProtocols expected)
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

        [ConditionalTheory]
        [InlineData("http1", HttpProtocols.Http1)]
        // [InlineData("http2", HttpProtocols.Http2)] // Not supported due to missing ALPN support. https://github.com/dotnet/corefx/issues/33016
        [InlineData("http1AndHttp2", HttpProtocols.Http1AndHttp2)] // Gracefully falls back to HTTP/1
        [OSSkipCondition(OperatingSystems.Linux)]
        [MaximumOSVersion(OperatingSystems.Windows, WindowsVersions.Win7)]
        public void EndpointConfigSectionCanSetProtocols_MacAndWin7(string input, HttpProtocols expected) =>
            EndpointConfigSectionCanSetProtocols(input, expected);

        [ConditionalTheory]
        [InlineData("http1", HttpProtocols.Http1)]
        [InlineData("http2", HttpProtocols.Http2)]
        [InlineData("http1AndHttp2", HttpProtocols.Http1AndHttp2)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win81)]
        public void EndpointConfigSectionCanSetProtocols_NonMacAndWin7(string input, HttpProtocols expected) =>
            EndpointConfigSectionCanSetProtocols(input, expected);

        private void EndpointConfigSectionCanSetProtocols(string input, HttpProtocols expected)
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

        [Fact]
        public void EndpointConfigureSection_CanSetSslProtocol()
        {
            var serverOptions = CreateServerOptions();
            var ranDefault = false;

            serverOptions.ConfigureHttpsDefaults(opt =>
            {
                opt.ServerCertificate = TestResources.GetTestCertificate();

                // Kestrel default
                Assert.Equal(SslProtocols.None, opt.SslProtocols);
                ranDefault = true;
            });

            var ran1 = false;
            var ran2 = false;
            var config = new ConfigurationBuilder().AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string>("Endpoints:End1:SslProtocols:0", "Tls11"),
                new KeyValuePair<string, string>("Endpoints:End1:Url", "https://*:5001"),
            }).Build();
            serverOptions.Configure(config)
                .Endpoint("End1", opt =>
                {
                    Assert.Equal(SslProtocols.Tls11, opt.HttpsOptions.SslProtocols);
                    ran1 = true;
                })
                .Load();
            serverOptions.ListenAnyIP(0, opt =>
            {
                opt.UseHttps(httpsOptions =>
                {
                    // Kestrel default.
                    Assert.Equal(SslProtocols.None, httpsOptions.SslProtocols);
                    ran2 = true;
                });
            });

            Assert.True(ranDefault);
            Assert.True(ran1);
            Assert.True(ran2);
        }

        [Fact]
        public void EndpointConfigureSection_CanOverrideSslProtocolsFromConfigureHttpsDefaults()
        {
            var serverOptions = CreateServerOptions();

            serverOptions.ConfigureHttpsDefaults(opt =>
            {
                opt.ServerCertificate = TestResources.GetTestCertificate();
                opt.SslProtocols = SslProtocols.Tls12;
            });

            var ran1 = false;
            var config = new ConfigurationBuilder().AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string>("Endpoints:End1:SslProtocols:0", "Tls11"),
                new KeyValuePair<string, string>("Endpoints:End1:Url", "https://*:5001"),
            }).Build();
            serverOptions.Configure(config)
                .Endpoint("End1", opt =>
                {
                    Assert.Equal(SslProtocols.Tls11, opt.HttpsOptions.SslProtocols);
                    ran1 = true;
                })
                .Load();

            Assert.True(ran1);
        }

        [Fact]
        public void DefaultEndpointConfigureSection_CanSetSslProtocols()
        {
            var serverOptions = CreateServerOptions();

            serverOptions.ConfigureHttpsDefaults(opt =>
            {
                opt.ServerCertificate = TestResources.GetTestCertificate();
            });

            var ran1 = false;
            var config = new ConfigurationBuilder().AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string>("EndpointDefaults:SslProtocols:0", "Tls11"),
                new KeyValuePair<string, string>("Endpoints:End1:Url", "https://*:5001"),
            }).Build();
            serverOptions.Configure(config)
                .Endpoint("End1", opt =>
                {
                    Assert.Equal(SslProtocols.Tls11, opt.HttpsOptions.SslProtocols);
                    ran1 = true;
                })
                .Load();

            Assert.True(ran1);
        }


        [Fact]
        public void DefaultEndpointConfigureSection_ConfigureHttpsDefaultsCanOverrideSslProtocols()
        {
            var serverOptions = CreateServerOptions();

            serverOptions.ConfigureHttpsDefaults(opt =>
            {
                opt.ServerCertificate = TestResources.GetTestCertificate();

                Assert.Equal(SslProtocols.Tls11, opt.SslProtocols);
                opt.SslProtocols = SslProtocols.Tls12;
            });

            var ran1 = false;
            var config = new ConfigurationBuilder().AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string>("EndpointDefaults:SslProtocols:0", "Tls11"),
                new KeyValuePair<string, string>("Endpoints:End1:Url", "https://*:5001"),
            }).Build();
            serverOptions.Configure(config)
                .Endpoint("End1", opt =>
                {
                    Assert.Equal(SslProtocols.Tls12, opt.HttpsOptions.SslProtocols);
                    ran1 = true;
                })
                .Load();

            Assert.True(ran1);
        }

        [Fact]
        public void Latin1RequestHeadersReadFromConfig()
        {
            var options = CreateServerOptions();
            var config =  new ConfigurationBuilder().AddInMemoryCollection().Build();

            Assert.False(options.Latin1RequestHeaders);
            options.Configure(config).Load();
            Assert.False(options.Latin1RequestHeaders);

            options = CreateServerOptions();
            config = new ConfigurationBuilder().AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string>("Latin1RequestHeaders", "true"),
            }).Build();

            Assert.False(options.Latin1RequestHeaders);
            options.Configure(config).Load();
            Assert.True(options.Latin1RequestHeaders);
            Assert.Same(KestrelServerOptions.DefaultLatin1RequestHeaderEncodingSelector, options.GetRequestHeaderEncodingSelector());
        }

        [Fact]
        public void Latin1RequestHeadersReadFromConfigCanBeOverriddenBySettingRequestHeaderEncodingSelector()
        {
            var options = CreateServerOptions();
            var config =  new ConfigurationBuilder().AddInMemoryCollection().Build();

            Assert.False(options.Latin1RequestHeaders);
            options.Configure(config).Load();
            Assert.False(options.Latin1RequestHeaders);

            options = CreateServerOptions();
            config = new ConfigurationBuilder().AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string>("Latin1RequestHeaders", "true"),
            }).Build();

            Assert.False(options.Latin1RequestHeaders);
            options.Configure(config).Load();
            Assert.True(options.Latin1RequestHeaders);

            Assert.NotSame(options.RequestHeaderEncodingSelector, options.GetRequestHeaderEncodingSelector());

            options.RequestHeaderEncodingSelector = _ => Encoding.ASCII;
            Assert.Same(options.RequestHeaderEncodingSelector, options.GetRequestHeaderEncodingSelector());
        }

        [Fact]
        public void Reload_IdentifiesEndpointsToStartAndStop()
        {
            var serverOptions = CreateServerOptions();

            var config = new ConfigurationBuilder().AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string>("Endpoints:A:Url", "http://*:5000"),
                new KeyValuePair<string, string>("Endpoints:B:Url", "http://*:5001"),
            }).Build();

            serverOptions.Configure(config).Load();

            Assert.Equal(2, serverOptions.ConfigurationBackedListenOptions.Count);
            Assert.Equal(5000, serverOptions.ConfigurationBackedListenOptions[0].IPEndPoint.Port);
            Assert.Equal(5001, serverOptions.ConfigurationBackedListenOptions[1].IPEndPoint.Port);

            serverOptions.ConfigurationLoader.Configuration = new ConfigurationBuilder().AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string>("Endpoints:A:Url", "http://*:5000"),
                new KeyValuePair<string, string>("Endpoints:B:Url", "http://*:5002"),
                new KeyValuePair<string, string>("Endpoints:C:Url", "http://*:5003"),
            }).Build();

            var (endpointsToStop, endpointsToStart) = serverOptions.ConfigurationLoader.Reload();

            Assert.Single(endpointsToStop);
            Assert.Equal(5001, endpointsToStop[0].IPEndPoint.Port);

            Assert.Equal(2, endpointsToStart.Count);
            Assert.Equal(5002, endpointsToStart[0].IPEndPoint.Port);
            Assert.Equal(5003, endpointsToStart[1].IPEndPoint.Port);

            Assert.Equal(3, serverOptions.ConfigurationBackedListenOptions.Count);
            Assert.Equal(5000, serverOptions.ConfigurationBackedListenOptions[0].IPEndPoint.Port);
            Assert.Same(endpointsToStart[0], serverOptions.ConfigurationBackedListenOptions[1]);
            Assert.Same(endpointsToStart[1], serverOptions.ConfigurationBackedListenOptions[2]);
        }

        [Fact]
        public void Reload_IdentifiesEndpointsWithChangedDefaults()
        {
            var serverOptions = CreateServerOptions();

            var config = new ConfigurationBuilder().AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string>("Endpoints:DefaultProtocol:Url", "http://*:5000"),
                new KeyValuePair<string, string>("Endpoints:NonDefaultProtocol:Url", "http://*:5001"),
                new KeyValuePair<string, string>("Endpoints:NonDefaultProtocol:Protocols", "Http1AndHttp2"),
            }).Build();

            serverOptions.Configure(config).Load();

            serverOptions.ConfigurationLoader.Configuration = new ConfigurationBuilder().AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string>("Endpoints:DefaultProtocol:Url", "http://*:5000"),
                new KeyValuePair<string, string>("Endpoints:NonDefaultProtocol:Url", "http://*:5001"),
                new KeyValuePair<string, string>("Endpoints:NonDefaultProtocol:Protocols", "Http1AndHttp2"),
                new KeyValuePair<string, string>("EndpointDefaults:Protocols", "Http1"),
            }).Build();

            var (endpointsToStop, endpointsToStart) = serverOptions.ConfigurationLoader.Reload();

            Assert.Single(endpointsToStop);
            Assert.Single(endpointsToStart);

            Assert.Equal(5000, endpointsToStop[0].IPEndPoint.Port);
            Assert.Equal(HttpProtocols.Http1AndHttp2, endpointsToStop[0].Protocols);
            Assert.Equal(5000, endpointsToStart[0].IPEndPoint.Port);
            Assert.Equal(HttpProtocols.Http1, endpointsToStart[0].Protocols);
        }

        [Fact]
        public void Reload_RerunsNamedEndpointConfigurationOnChange()
        {
            var foundChangedCount = 0;
            var foundUnchangedCount = 0;
            var serverOptions = CreateServerOptions();

            var config = new ConfigurationBuilder().AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string>("Endpoints:Changed:Url", "http://*:5001"),
                new KeyValuePair<string, string>("Endpoints:Unchanged:Url", "http://*:5000"),
            }).Build();

            serverOptions.Configure(config)
                .Endpoint("Changed", endpointOptions => foundChangedCount++)
                .Endpoint("Unchanged", endpointOptions => foundUnchangedCount++)
                .Endpoint("NotFound", endpointOptions => throw new NotImplementedException())
                .Load();

            Assert.Equal(1, foundChangedCount);
            Assert.Equal(1, foundUnchangedCount);

            serverOptions.ConfigurationLoader.Configuration = new ConfigurationBuilder().AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string>("Endpoints:Changed:Url", "http://*:5002"),
                new KeyValuePair<string, string>("Endpoints:Unchanged:Url", "http://*:5000"),
            }).Build();

            serverOptions.ConfigurationLoader.Reload();

            Assert.Equal(2, foundChangedCount);
            Assert.Equal(1, foundUnchangedCount);
        }

        private static string GetCertificatePath()
        {
            var appData = Environment.GetEnvironmentVariable("APPDATA");
            var home = Environment.GetEnvironmentVariable("HOME");
            var basePath = appData != null ? Path.Combine(appData, "ASP.NET", "https") : null;
            basePath = basePath ?? (home != null ? Path.Combine(home, ".aspnet", "https") : null);
            return Path.Combine(basePath, $"TestApplication.pfx");
        }

        private class MockHostingEnvironment : IHostEnvironment
        {
            public string ApplicationName { get; set; }
            public string EnvironmentName { get; set; }
            public string ContentRootPath { get; set; }
            public IFileProvider ContentRootFileProvider { get; set; }
        }
    }
}

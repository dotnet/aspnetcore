// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Primitives;
using Moq;

namespace Microsoft.AspNetCore.Server.Kestrel.Tests;

public class KestrelConfigurationLoaderTests
{
    private KestrelServerOptions CreateServerOptions()
    {
        var serverOptions = new KestrelServerOptions();
        var env = new MockHostingEnvironment { ApplicationName = "TestApplication", ContentRootPath = Directory.GetCurrentDirectory() };
        serverOptions.ApplicationServices = new ServiceCollection()
            .AddLogging()
            .AddSingleton<IHostEnvironment>(env)
            .AddSingleton(new KestrelMetrics(new TestMeterFactory()))
            .AddSingleton<IHttpsConfigurationService, HttpsConfigurationService>()
            .AddSingleton<HttpsConfigurationService.IInitializer, HttpsConfigurationService.Initializer>()
            .BuildServiceProvider();
        return serverOptions;
    }

    private static Mock<IConfiguration> CreateMockConfiguration() => CreateMockConfiguration(out _);

    private static Mock<IConfiguration> CreateMockConfiguration(out Mock<IChangeToken> mockReloadToken)
    {
        var currentConfig = new ConfigurationBuilder().AddInMemoryCollection(new[]
{
            new KeyValuePair<string, string>("Endpoints:A:Url", "http://*:5000"),
            new KeyValuePair<string, string>("Endpoints:B:Url", "http://*:5001"),
        }).Build();

        mockReloadToken = new Mock<IChangeToken>();

        var mockConfig = new Mock<IConfiguration>();
        mockConfig.Setup(c => c.GetSection(It.IsAny<string>())).Returns<string>(currentConfig.GetSection);
        mockConfig.Setup(c => c.GetChildren()).Returns(currentConfig.GetChildren);
        mockConfig.Setup(c => c.GetReloadToken()).Returns(mockReloadToken.Object);

        return mockConfig;
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

        Assert.Single(serverOptions.GetListenOptions());
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

        Assert.Empty(serverOptions.GetListenOptions());

        serverOptions.ConfigurationLoader.Load();

        Assert.Single(serverOptions.GetListenOptions());
        Assert.Equal(5001, serverOptions.CodeBackedListenOptions[0].IPEndPoint.Port);

        Assert.True(run);
    }

    [Fact]
    public void CallBuildTwice_OnlyRunsOnce()
    {
        var serverOptions = CreateServerOptions();
        var builder = serverOptions.Configure()
            .LocalhostEndpoint(5001);

        Assert.Empty(serverOptions.GetListenOptions());
        Assert.Equal(builder, serverOptions.ConfigurationLoader);

        builder.Load();

        Assert.Single(serverOptions.GetListenOptions());
        Assert.Equal(5001, serverOptions.CodeBackedListenOptions[0].IPEndPoint.Port);
        Assert.NotNull(serverOptions.ConfigurationLoader);

        builder.Load();

        Assert.Single(serverOptions.GetListenOptions());
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

        Assert.Empty(serverOptions.GetListenOptions());
        Assert.False(run1);

        var run2 = false;
        var config2 = new ConfigurationBuilder().AddInMemoryCollection(new[]
        {
            new KeyValuePair<string, string>("Endpoints:End2:Url", "http://*:5002"),
        }).Build();
        serverOptions.Configure(config2)
            .LocalhostEndpoint(5003, endpointOptions => run2 = true);

        serverOptions.ConfigurationLoader.Load();

        Assert.Equal(2, serverOptions.GetListenOptions().Length);
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
            opt.ServerCertificateChain = TestResources.GetTestChain();
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
                Assert.NotNull(opt.HttpsOptions.ServerCertificateChain);
                Assert.Equal(2, opt.HttpsOptions.ServerCertificateChain.Count);
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
    public void ConfigureDefaultCertificatePathLoadsChain()
    {
        var serverOptions = CreateServerOptions();
        var testCertPath = TestResources.GetCertPath("leaf.com.crt");
        var ran1 = false;
        var config = new ConfigurationBuilder().AddInMemoryCollection(new[]
        {
            new KeyValuePair<string, string>("Endpoints:End1:Url", "https://*:5001"),
            new KeyValuePair<string,string>("Certificates:Default:Path", testCertPath)
        }).Build();

        serverOptions.Configure(config)
            .Endpoint("End1", opt =>
            {
                ran1 = true;
                Assert.True(opt.IsHttps);
                Assert.NotNull(opt.HttpsOptions.ServerCertificate);
                Assert.NotNull(opt.HttpsOptions.ServerCertificateChain);
                Assert.Equal(2, opt.HttpsOptions.ServerCertificateChain.Count);
            }).Load();

        Assert.True(ran1);

        Assert.True(serverOptions.ConfigurationBackedListenOptions[0].IsTls);
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
    // inherently flaky (writes to a well-known path)
    [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/48736")]
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
            Assert.Null(serverOptions.DevelopmentCertificate); // Not used since configuration cert is present
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
    public void DevelopmentCertificateCanBeRemoved()
    {
        try
        {
            var serverOptions = CreateServerOptions();

            var devCert = new X509Certificate2(TestResources.GetCertPath("aspnetdevcert.pfx"), "testPassword", X509KeyStorageFlags.Exportable);
            var devCertBytes = devCert.Export(X509ContentType.Pkcs12, "1234");
            var devCertPath = GetCertificatePath();
            Directory.CreateDirectory(Path.GetDirectoryName(devCertPath));
            File.WriteAllBytes(devCertPath, devCertBytes);

            var defaultCertPath = TestResources.TestCertificatePath;
            var defaultCert = TestResources.GetTestCertificate();
            Assert.NotEqual(devCert.SerialNumber, defaultCert.SerialNumber); // Need to be able to distinguish them

            var endpointConfig = new[]
            {
                new KeyValuePair<string, string>("Endpoints:End1:Url", "https://*:5001"),
            };
            var devCertConfig = new[]
            {
                new KeyValuePair<string, string>("Certificates:Development:Password", "1234"),
            };
            var defaultCertConfig = new[]
            {
                new KeyValuePair<string, string>("Certificates:Default:path", defaultCertPath),
                new KeyValuePair<string, string>("Certificates:Default:Password", "testPassword"),
            };

            var config = new ConfigurationBuilder().AddInMemoryCollection(endpointConfig.Concat(devCertConfig)).Build();

            serverOptions.Configure(config).Load();

            CheckCertificates(devCert);

            // Add Default certificate
            serverOptions.ConfigurationLoader.Configuration = new ConfigurationBuilder().AddInMemoryCollection(endpointConfig.Concat(devCertConfig).Concat(defaultCertConfig)).Build();
            _ = serverOptions.ConfigurationLoader.Reload();

            // Default is preferred to Development
            CheckCertificates(defaultCert);

            // Remove Default certificate
            serverOptions.ConfigurationLoader.Configuration = new ConfigurationBuilder().AddInMemoryCollection(endpointConfig.Concat(devCertConfig)).Build();
            _ = serverOptions.ConfigurationLoader.Reload();

            // Back to Development
            CheckCertificates(devCert);

            // Remove Development certificate
            serverOptions.ConfigurationLoader.Configuration = new ConfigurationBuilder().AddInMemoryCollection(endpointConfig).Build();

            // With all of the configuration certs removed, the only place left to check is the CertificateManager.
            // We don't want to depend on machine state, so we cheat and say we already looked.
            serverOptions.IsDevelopmentCertificateLoaded = true;
            Assert.Null(serverOptions.DevelopmentCertificate);

            // Since there are no configuration certs and we bypassed the CertificateManager, there will be an
            // exception about not finding any certs at all.
            Assert.Throws<InvalidOperationException>(() => serverOptions.ConfigurationLoader.Reload());

            Assert.Null(serverOptions.ConfigurationLoader.DefaultCertificate);

            void CheckCertificates(X509Certificate2 expectedCert)
            {
                var httpsOptions = new HttpsConnectionAdapterOptions();
                serverOptions.ApplyDefaultCertificate(httpsOptions);
                Assert.Equal(expectedCert.SerialNumber, httpsOptions.ServerCertificate.SerialNumber);
                Assert.Equal(expectedCert.SerialNumber, serverOptions.ConfigurationLoader.DefaultCertificate.SerialNumber);
            }
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
    public void ConfigureEndpoint_RecoverFromBadPassword()
    {
        var serverOptions = CreateServerOptions();

        var configRoot = new ConfigurationBuilder().AddInMemoryCollection(new[]
        {
            new KeyValuePair<string, string>("Endpoints:End1:Url", "https://*:5001"),
            new KeyValuePair<string, string>("Endpoints:End1:Certificate:Path", TestResources.TestCertificatePath),
            new KeyValuePair<string, string>("Endpoints:End1:Certificate:Password", "testPassword")
        }).Build();
        var configProvider = configRoot.Providers.Single();

        var testCertificate = TestResources.GetTestCertificate();

        var otherCertificatePath = TestResources.GetCertPath("aspnetdevcert.pfx");
        var otherCertificate = new X509Certificate2(otherCertificatePath, "testPassword");

        serverOptions.Configure(configRoot).Load();
        CheckListenOptions(testCertificate);

        // Update cert but use incorrect password
        configProvider.Set("Endpoints:End1:Certificate:Path", otherCertificatePath);
        configProvider.Set("Endpoints:End1:Certificate:Password", "badPassword");

        // Fails to load certificate because password is bad
        Assert.ThrowsAny<CryptographicException>(() => serverOptions.ConfigurationLoader.Reload());

        // ConfigurationBackedListenOptions still contains prior value
        CheckListenOptions(testCertificate);

        // Correct password
        configProvider.Set("Endpoints:End1:Certificate:Password", "testPassword");
        _ = serverOptions.ConfigurationLoader.Reload();

        // ConfigurationBackedListenOptions contains new value
        CheckListenOptions(otherCertificate);

        void CheckListenOptions(X509Certificate2 expectedCert)
        {
            var listenOptions = Assert.Single(serverOptions.ConfigurationBackedListenOptions);
            Assert.Equal(expectedCert.SerialNumber, listenOptions.HttpsOptions!.ServerCertificate.SerialNumber);
        }
    }

    [Fact]
    public void LoadDevelopmentCertificate_LoadBeforeUseHttps()
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
                new KeyValuePair<string, string>("Certificates:Development:Password", "1234"),
            }).Build();

            serverOptions.Configure(config);

            Assert.Null(serverOptions.ConfigurationLoader.DefaultCertificate);

            serverOptions.ConfigurationLoader.Load();

            Assert.NotNull(serverOptions.ConfigurationLoader.DefaultCertificate);
            Assert.Equal(serverOptions.ConfigurationLoader.DefaultCertificate.SerialNumber, certificate.SerialNumber);

            var ran1 = false;
            serverOptions.ListenAnyIP(4545, listenOptions =>
            {
                ran1 = true;
                listenOptions.UseHttps();
            });
            Assert.True(ran1);

            var listenOptions = serverOptions.CodeBackedListenOptions.Single();
            listenOptions.Build();
            Assert.Equal(listenOptions.HttpsOptions.ServerCertificate?.SerialNumber, certificate.SerialNumber);
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
    public void LoadDevelopmentCertificate_UseHttpsBeforeLoad()
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
                new KeyValuePair<string, string>("Certificates:Development:Password", "1234"),
            }).Build();

            serverOptions.Configure(config);

            Assert.Null(serverOptions.ConfigurationLoader.DefaultCertificate);

            var ran1 = false;
            serverOptions.ListenAnyIP(4545, listenOptions =>
            {
                ran1 = true;
                listenOptions.UseHttps();
            });
            Assert.True(ran1);

            // Use Https triggers a load, so the default cert is already set
            Assert.NotNull(serverOptions.ConfigurationLoader.DefaultCertificate);
            Assert.Equal(serverOptions.ConfigurationLoader.DefaultCertificate.SerialNumber, certificate.SerialNumber);

            // This Load is a no-op (tested elsewhere)
            serverOptions.ConfigurationLoader.Load();

            var listenOptions = serverOptions.CodeBackedListenOptions.Single();
            listenOptions.Build();
            Assert.Equal(listenOptions.HttpsOptions.ServerCertificate?.SerialNumber, certificate.SerialNumber);
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
    public void LoadDevelopmentCertificate_UseHttpsBeforeConfigure()
    {
        try
        {
            var serverOptions = CreateServerOptions();
            var certificate = new X509Certificate2(TestResources.GetCertPath("aspnetdevcert.pfx"), "testPassword", X509KeyStorageFlags.Exportable);
            var bytes = certificate.Export(X509ContentType.Pkcs12, "1234");
            var path = GetCertificatePath();
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, bytes);

            var defaultCertificate = TestResources.GetTestCertificate();
            Assert.NotEqual(certificate.SerialNumber, defaultCertificate.SerialNumber);
            serverOptions.TestOverrideDefaultCertificate = defaultCertificate;

            var ran1 = false;
            serverOptions.ListenAnyIP(4545, listenOptions =>
            {
                ran1 = true;
                listenOptions.UseHttps();
            });
            Assert.True(ran1);

            var config = new ConfigurationBuilder().AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string>("Certificates:Development:Password", "1234"),
            }).Build();

            serverOptions.Configure(config);

            Assert.Null(serverOptions.ConfigurationLoader.DefaultCertificate);

            serverOptions.ConfigurationLoader.Load();

            Assert.NotNull(serverOptions.ConfigurationLoader.DefaultCertificate);
            Assert.Equal(serverOptions.ConfigurationLoader.DefaultCertificate.SerialNumber, certificate.SerialNumber);

            var listenOptions = serverOptions.CodeBackedListenOptions.Single();
            listenOptions.Build();
            // In a perfect world, it would match certificate.SerialNumber, but there's no way for an eager UseHttps
            // to do that before Configure is called.
            Assert.Equal(listenOptions.HttpsOptions.ServerCertificate?.SerialNumber, defaultCertificate.SerialNumber);
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
    public void ConfigureEndpoint_ThrowsWhen_The_PasswordIsMissing()
    {
        var serverOptions = CreateServerOptions();
        var certificate = new X509Certificate2(TestResources.GetCertPath("https-aspnet.crt"));

        var config = new ConfigurationBuilder().AddInMemoryCollection(new[]
        {
            new KeyValuePair<string, string>("Endpoints:End1:Url", "https://*:5001"),
            new KeyValuePair<string, string>("Certificates:Default:Path", Path.Combine("shared", "TestCertificates", "https-aspnet.crt")),
            new KeyValuePair<string, string>("Certificates:Default:KeyPath", Path.Combine("shared", "TestCertificates", "https-aspnet.key"))
        }).Build();

        var ex = Assert.Throws<ArgumentException>(() =>
        {
            serverOptions
                .Configure(config)
                .Endpoint("End1", opt =>
                {
                    Assert.True(opt.IsHttps);
                }).Load();
        });
    }

    [Fact]
    public void ConfigureEndpoint_ThrowsWhen_TheKeyDoesntMatchTheCertificateKey()
    {
        var serverOptions = CreateServerOptions();
        var certificate = new X509Certificate2(TestResources.GetCertPath("https-aspnet.crt"));

        var config = new ConfigurationBuilder().AddInMemoryCollection(new[]
        {
            new KeyValuePair<string, string>("Endpoints:End1:Url", "https://*:5001"),
            new KeyValuePair<string, string>("Certificates:Default:Path", Path.Combine("shared", "TestCertificates", "https-aspnet.crt")),
            new KeyValuePair<string, string>("Certificates:Default:KeyPath", Path.Combine("shared", "TestCertificates", "https-ecdsa.key")),
            new KeyValuePair<string, string>("Certificates:Default:Password", "aspnetcore")
        }).Build();

        var ex = Assert.Throws<ArgumentException>(() =>
        {
            serverOptions
                .Configure(config)
                .Endpoint("End1", opt =>
                {
                    Assert.True(opt.IsHttps);
                }).Load();
        });
    }

    [Fact]
    public void ConfigureEndpoint_ThrowsWhen_The_PasswordIsIncorrect()
    {
        var serverOptions = CreateServerOptions();
        var certificate = new X509Certificate2(TestResources.GetCertPath("https-aspnet.crt"));

        var config = new ConfigurationBuilder().AddInMemoryCollection(new[]
        {
                    new KeyValuePair<string, string>("Endpoints:End1:Url", "https://*:5001"),
                    new KeyValuePair<string, string>("Certificates:Default:Path", Path.Combine("shared", "TestCertificates", "https-aspnet.crt")),
                    new KeyValuePair<string, string>("Certificates:Default:KeyPath", Path.Combine("shared", "TestCertificates", "https-aspnet.key")),
                    new KeyValuePair<string, string>("Certificates:Default:Password", "abcde"),
                }).Build();

        var ex = Assert.Throws<CryptographicException>(() =>
        {
            serverOptions
                .Configure(config)
                .Endpoint("End1", opt =>
                {
                    Assert.True(opt.IsHttps);
                }).Load();
        });
    }

    [Fact]
    public void ConfigureEndpoint_ThrowsWhen_The_KeyIsPublic()
    {
        var serverOptions = CreateServerOptions();
        var certificate = new X509Certificate2(TestResources.GetCertPath("https-aspnet.crt"));

        var config = new ConfigurationBuilder().AddInMemoryCollection(new[]
        {
                    new KeyValuePair<string, string>("Endpoints:End1:Url", "https://*:5001"),
                    new KeyValuePair<string, string>("Certificates:Default:Path", Path.Combine("shared", "TestCertificates", "https-aspnet.crt")),
                    new KeyValuePair<string, string>("Certificates:Default:KeyPath", Path.Combine("shared", "TestCertificates", "https-aspnet.pub")),
                }).Build();

        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            serverOptions
                .Configure(config)
                .Endpoint("End1", opt =>
                {
                    Assert.True(opt.IsHttps);
                }).Load();
        });
        Assert.StartsWith("Error getting private key from", ex.Message);
        Assert.IsAssignableFrom<CryptographicException>(ex.InnerException);
    }

#pragma warning disable SYSLIB5006
    private static readonly Dictionary<string, MLDsaAlgorithm> _mlDsaAlgorithms = ((IEnumerable<MLDsaAlgorithm>)[
        MLDsaAlgorithm.MLDsa44,
        MLDsaAlgorithm.MLDsa65,
        MLDsaAlgorithm.MLDsa87,
    ]).ToDictionary(a => a.Name);

    private static readonly Dictionary<string, SlhDsaAlgorithm> _slhDsaAlgorithms = ((IEnumerable<SlhDsaAlgorithm>)[
        SlhDsaAlgorithm.SlhDsaSha2_128s,
        SlhDsaAlgorithm.SlhDsaSha2_128f,
        SlhDsaAlgorithm.SlhDsaSha2_192s,
        SlhDsaAlgorithm.SlhDsaSha2_192f,
        SlhDsaAlgorithm.SlhDsaSha2_256s,
        SlhDsaAlgorithm.SlhDsaSha2_256f,
        SlhDsaAlgorithm.SlhDsaShake128s,
        SlhDsaAlgorithm.SlhDsaShake128f,
        SlhDsaAlgorithm.SlhDsaShake192s,
        SlhDsaAlgorithm.SlhDsaShake192f,
        SlhDsaAlgorithm.SlhDsaShake256s,
        SlhDsaAlgorithm.SlhDsaShake256f,
    ]).ToDictionary(a => a.Name);

    private static readonly Dictionary<string, CompositeMLDsaAlgorithm> _compositeMLDsaAlgorithms = ((IEnumerable<CompositeMLDsaAlgorithm>)[
        CompositeMLDsaAlgorithm.MLDsa65WithECDsaP256,
        CompositeMLDsaAlgorithm.MLDsa65WithRSA3072Pkcs15,
        CompositeMLDsaAlgorithm.MLDsa65WithEd25519,
        CompositeMLDsaAlgorithm.MLDsa65WithECDsaP384,
        CompositeMLDsaAlgorithm.MLDsa87WithRSA4096Pss,
        CompositeMLDsaAlgorithm.MLDsa65WithECDsaBrainpoolP256r1,
        CompositeMLDsaAlgorithm.MLDsa44WithRSA2048Pss,
        CompositeMLDsaAlgorithm.MLDsa44WithRSA2048Pkcs15,
        CompositeMLDsaAlgorithm.MLDsa44WithEd25519,
        CompositeMLDsaAlgorithm.MLDsa44WithECDsaP256,
        CompositeMLDsaAlgorithm.MLDsa65WithRSA4096Pss,
        CompositeMLDsaAlgorithm.MLDsa87WithECDsaBrainpoolP384r1,
        CompositeMLDsaAlgorithm.MLDsa87WithECDsaP384,
        CompositeMLDsaAlgorithm.MLDsa87WithECDsaP521,
        CompositeMLDsaAlgorithm.MLDsa87WithEd448,
        CompositeMLDsaAlgorithm.MLDsa87WithRSA3072Pss,
        CompositeMLDsaAlgorithm.MLDsa65WithRSA3072Pss,
        CompositeMLDsaAlgorithm.MLDsa65WithRSA4096Pkcs15,
    ]).ToDictionary(a => a.Name);
#pragma warning restore SYSLIB5006

    public static TheoryData<string, string, string> GetPemCertificateTestData()
    {
        var data = new TheoryData<string, string, string>();
        List<string> algorithms = [
            "RSA",
            "ECDsa",
            "DSA",
        ];

#pragma warning disable SYSLIB5006
        if (MLDsa.IsSupported)
        {
            algorithms.AddRange(_mlDsaAlgorithms.Keys);
        }

        if (SlhDsa.IsSupported)
        {
            algorithms.AddRange(_slhDsaAlgorithms.Keys);
        }

        // Composite ML-DSA certificate generation is not supported at the time
        // of writing, so we skip it.
        // When it gets implemented in the future, simply remove the SkipCompositeMLDsa
        // condition to include it in the tests.
        const bool SkipCompositeMLDsa = true;
        if (CompositeMLDsa.IsSupported && !SkipCompositeMLDsa)
        {
            algorithms.AddRange(_compositeMLDsaAlgorithms
                .Where(kvp => CompositeMLDsa.IsAlgorithmSupported(kvp.Value))
                .Select(kvp => kvp.Key));
        }
#pragma warning restore SYSLIB5006

        foreach (var algorithm in algorithms)
        {
            // Test with no password
            data.Add(algorithm, null, ".pem");
            data.Add(algorithm, null, ".crt");

            // Test with password
            data.Add(algorithm, "test", ".pem");
            data.Add(algorithm, "test", ".crt");
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(GetPemCertificateTestData))]
    [QuarantinedTest("https://github.com/dotnet/aspnetcore/pull/63708/")]
    public void ConfigureEndpoint_CanLoadPemCertificates(string algorithmType, string keyPassword, string extension)
    {
        var serverOptions = CreateServerOptions();
        X509Certificate2 certificate;
        string certificateFilePath;
        string certificateKeyPath;
        string tempDir = null;

        // For DSA, we rely on a pre-generated certificate since CertificateRequest APIs don't directly
        // support DSA keys.
        if (algorithmType == "DSA")
        {
            var baseName = keyPassword == null ? "https-dsa" : "https-dsa";
            var keyName = keyPassword == null ? "https-dsa.key" : "https-dsa-protected.key";

            certificate = new X509Certificate2(TestResources.GetCertPath("https-dsa.crt"));
            certificateFilePath = TestResources.GetCertPath(baseName + extension);
            certificateKeyPath = TestResources.GetCertPath(keyName);
        }
        else
        {
            // For all other algorithms, we generate the certificate and key dynamically, saving them
            // in a temporary directory.
            tempDir = Directory.CreateTempSubdirectory().FullName;
            try
            {
                certificateFilePath = Path.Combine(tempDir, $"test{extension}");
                certificateKeyPath = Path.Combine(tempDir, "test.key");
                certificate = GenerateTestCertificateWithAlgorithm(algorithmType, keyPassword, certificateFilePath, certificateKeyPath);
            }
            catch
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, recursive: true);
                }
                throw;
            }
        }

        try
        {
            var ran1 = false;
            var config = new ConfigurationBuilder().AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string>("Endpoints:End1:Url", "https://*:5001"),
                new KeyValuePair<string, string>("Certificates:Default:Path", certificateFilePath),
                new KeyValuePair<string, string>("Certificates:Default:KeyPath", certificateKeyPath),
            }
            .Concat(keyPassword != null ? [new KeyValuePair<string, string>("Certificates:Default:Password", keyPassword)] : Array.Empty<KeyValuePair<string, string>>()))
            .Build();

            serverOptions
                .Configure(config)
                .Endpoint("End1", opt =>
                {
                    ran1 = true;
                    Assert.True(opt.IsHttps);
                    Assert.Equal(certificate.SerialNumber, opt.HttpsOptions.ServerCertificate.SerialNumber);
                }).Load();

            Assert.True(ran1);
            Assert.Null(serverOptions.DevelopmentCertificate); // Not used since configuration cert is present
        }
        finally
        {
            // Clean up generated certificate directory if it exists.
            if (tempDir is not null && Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    private static X509Certificate2 GenerateTestCertificateWithAlgorithm(string algorithmType, string keyPassword, string certificatePath, string keyPath)
    {
        var distinguishedName = new X500DistinguishedName($"CN=test.{algorithmType}.local");
        var sanBuilder = new SubjectAlternativeNameBuilder();
        sanBuilder.AddDnsName($"test.{algorithmType}.local");

        X509Certificate2 certificate;
        string keyPem;

        switch (algorithmType)
        {
            case "RSA":
                using (var rsa = RSA.Create(2048))
                {
                    var request = new CertificateRequest(distinguishedName, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                    certificate = CreateTestCertificate(request, sanBuilder);
                    keyPem = ExportKeyToPem(rsa, keyPassword);
                }
                break;

            case "ECDsa":
                using (var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256))
                {
                    var request = new CertificateRequest(distinguishedName, ecdsa, HashAlgorithmName.SHA256);
                    certificate = CreateTestCertificate(request, sanBuilder);
                    keyPem = ExportKeyToPem(ecdsa, keyPassword);
                }
                break;

#pragma warning disable SYSLIB5006
            case var x when _mlDsaAlgorithms.TryGetValue(x, out var mlDsaAlgorithm):
                using (var mlDsa = MLDsa.GenerateKey(mlDsaAlgorithm))
                {
                    var request = new CertificateRequest(distinguishedName, mlDsa);
                    certificate = CreateTestCertificate(request, sanBuilder);
                    keyPem = ExportMLDsaKeyToPem(mlDsa, keyPassword);
                }
                break;

            case var x when _slhDsaAlgorithms.TryGetValue(x, out var slhDsaAlgorithm):
                using (var slhDsa = SlhDsa.GenerateKey(slhDsaAlgorithm))
                {
                    var request = new CertificateRequest(distinguishedName, slhDsa);
                    certificate = CreateTestCertificate(request, sanBuilder);
                    keyPem = ExportSlhDsaKeyToPem(slhDsa, keyPassword);
                }
                break;

            case var x when _compositeMLDsaAlgorithms.TryGetValue(x, out var compositeMLDsaAlgorithm):
                using (var compositeMLDsa = CompositeMLDsa.GenerateKey(compositeMLDsaAlgorithm))
                {
                    var request = new CertificateRequest(distinguishedName, compositeMLDsa);
                    certificate = CreateTestCertificate(request, sanBuilder);
                    keyPem = ExportCompositeMLDsaKeyToPem(compositeMLDsa, keyPassword);
                }
                break;
#pragma warning restore SYSLIB5006

            default:
                throw new ArgumentException($"Unknown algorithm type: {algorithmType}");
        }

        if (certificatePath.EndsWith(".pem", StringComparison.OrdinalIgnoreCase))
        {
            // Export the certificate in PEM format
            File.WriteAllText(certificatePath, certificate.ExportCertificatePem());
        }
        else
        {
            // Export the certificate in DER format
            File.WriteAllBytes(certificatePath, certificate.Export(X509ContentType.Cert));
        }

        File.WriteAllText(keyPath, keyPem);

        return certificate;
    }

    private static X509Certificate2 CreateTestCertificate(CertificateRequest request, SubjectAlternativeNameBuilder sanBuilder)
    {
        request.CertificateExtensions.Add(sanBuilder.Build());
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, critical: true));
        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension([new Oid("1.3.6.1.5.5.7.3.1")], critical: false)); // Server Authentication

        var notBefore = DateTimeOffset.UtcNow.AddDays(-1);
        var notAfter = DateTimeOffset.UtcNow.AddYears(1);

        return request.CreateSelfSigned(notBefore, notAfter);
    }

    private static string ExportKeyToPem(AsymmetricAlgorithm key, string password)
    {
        return password is null
            ? key.ExportPkcs8PrivateKeyPem()
            : key.ExportEncryptedPkcs8PrivateKeyPem(password.AsSpan(), new PbeParameters(PbeEncryptionAlgorithm.Aes256Cbc, HashAlgorithmName.SHA256, 100_000));
    }

#pragma warning disable SYSLIB5006
    private static string ExportMLDsaKeyToPem(MLDsa mlDsa, string password)
    {
        return password is null
            ? mlDsa.ExportPkcs8PrivateKeyPem()
            : mlDsa.ExportEncryptedPkcs8PrivateKeyPem(password.AsSpan(), new PbeParameters(PbeEncryptionAlgorithm.Aes256Cbc, HashAlgorithmName.SHA256, 100_000));
    }

    private static string ExportSlhDsaKeyToPem(SlhDsa slhDsa, string password)
    {
        return password is null
            ? slhDsa.ExportPkcs8PrivateKeyPem()
            : slhDsa.ExportEncryptedPkcs8PrivateKeyPem(password.AsSpan(), new PbeParameters(PbeEncryptionAlgorithm.Aes256Cbc, HashAlgorithmName.SHA256, 100_000));
    }

    private static string ExportCompositeMLDsaKeyToPem(CompositeMLDsa compositeMLDsa, string password)
    {
        return password is null
            ? compositeMLDsa.ExportPkcs8PrivateKeyPem()
            : compositeMLDsa.ExportEncryptedPkcs8PrivateKeyPem(password.AsSpan(), new PbeParameters(PbeEncryptionAlgorithm.Aes256Cbc, HashAlgorithmName.SHA256, 100_000));
    }

#pragma warning restore SYSLIB5006

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

            Assert.Null(serverOptions.DevelopmentCertificate);
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

            Assert.Null(serverOptions.DevelopmentCertificate);
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
    public void ConfigureEndpoint_ThrowsWhen_HttpsConfigIsDeclaredInNonHttpsEndpoints()
    {
        var serverOptions = CreateServerOptions();

        var config = new ConfigurationBuilder().AddInMemoryCollection(new[]
        {
            new KeyValuePair<string, string>("Endpoints:End1:Url", "http://*:5001"),
            // We shouldn't need to specify a real cert, because KestrelConfigurationLoader should check whether the endpoint requires a cert before trying to load it.
            new KeyValuePair<string, string>("Endpoints:End1:Certificate:Path", "fakecert.pfx"),
        }).Build();

        var ex = Assert.Throws<InvalidOperationException>(() => serverOptions.Configure(config).Load());
        Assert.Equal(CoreStrings.FormatEndpointHasUnusedHttpsConfig("End1", "Certificate"), ex.Message);

        config = new ConfigurationBuilder().AddInMemoryCollection(new[]
        {
            new KeyValuePair<string, string>("Endpoints:End1:Url", "http://*:5001"),
            new KeyValuePair<string, string>("Endpoints:End1:Certificate:Subject", "example.org"),
        }).Build();

        ex = Assert.Throws<InvalidOperationException>(() => serverOptions.Configure(config).Load());
        Assert.Equal(CoreStrings.FormatEndpointHasUnusedHttpsConfig("End1", "Certificate"), ex.Message);

        config = new ConfigurationBuilder().AddInMemoryCollection(new[]
        {
            new KeyValuePair<string, string>("Endpoints:End1:Url", "http://*:5001"),
            new KeyValuePair<string, string>("Endpoints:End1:ClientCertificateMode", ClientCertificateMode.RequireCertificate.ToString()),
        }).Build();

        ex = Assert.Throws<InvalidOperationException>(() => serverOptions.Configure(config).Load());
        Assert.Equal(CoreStrings.FormatEndpointHasUnusedHttpsConfig("End1", "ClientCertificateMode"), ex.Message);

        config = new ConfigurationBuilder().AddInMemoryCollection(new[]
        {
            new KeyValuePair<string, string>("Endpoints:End1:Url", "http://*:5001"),
            new KeyValuePair<string, string>("Endpoints:End1:SslProtocols:0", SslProtocols.Tls13.ToString()),
        }).Build();

        ex = Assert.Throws<InvalidOperationException>(() => serverOptions.Configure(config).Load());
        Assert.Equal(CoreStrings.FormatEndpointHasUnusedHttpsConfig("End1", "SslProtocols"), ex.Message);

        config = new ConfigurationBuilder().AddInMemoryCollection(new[]
        {
            new KeyValuePair<string, string>("Endpoints:End1:Url", "http://*:5001"),
            new KeyValuePair<string, string>("Endpoints:End1:Sni:Protocols", HttpProtocols.Http1.ToString()),
        }).Build();

        ex = Assert.Throws<InvalidOperationException>(() => serverOptions.Configure(config).Load());
        Assert.Equal(CoreStrings.FormatEndpointHasUnusedHttpsConfig("End1", "Sni"), ex.Message);
    }

    [Fact]
    public void ConfigureEndpoint_DoesNotThrowWhen_HttpsConfigIsDeclaredInEndpointDefaults()
    {
        var serverOptions = CreateServerOptions();

        var config = new ConfigurationBuilder().AddInMemoryCollection(new[]
        {
            new KeyValuePair<string, string>("Endpoints:End1:Url", "http://*:5001"),
            new KeyValuePair<string, string>("EndpointDefaults:ClientCertificateMode", ClientCertificateMode.RequireCertificate.ToString()),
        }).Build();

        var (_, endpointsToStart) = serverOptions.Configure(config).Reload();
        var end1 = Assert.Single(endpointsToStart);
        Assert.NotNull(end1?.EndpointConfig);
        Assert.Null(end1.EndpointConfig.ClientCertificateMode);

        serverOptions = CreateServerOptions();

        config = new ConfigurationBuilder().AddInMemoryCollection(new[]
        {
                new KeyValuePair<string, string>("Endpoints:End1:Url", "http://*:5001"),
                new KeyValuePair<string, string>("EndpointDefaults:SslProtocols:0", SslProtocols.Tls13.ToString()),
            }).Build();

        (_, endpointsToStart) = serverOptions.Configure(config).Reload();
        end1 = Assert.Single(endpointsToStart);
        Assert.NotNull(end1?.EndpointConfig);
        Assert.Null(end1.EndpointConfig.SslProtocols);
    }

    // On helix retry list - inherently flaky (FS events)
    [Theory]
    [InlineData(true)] // This might be flaky, since it depends on file system events (or polling)
    [InlineData(false)] // This will be slow (1 seconds)
    public async Task CertificateChangedOnDisk(bool reloadOnChange)
    {
        var certificatePath = GetCertificatePath();

        try
        {
            var serverOptions = CreateServerOptions();

            var certificatePassword = "1234";

            var oldCertificate = new X509Certificate2(TestResources.GetCertPath("aspnetdevcert.pfx"), "testPassword", X509KeyStorageFlags.Exportable);
            var oldCertificateBytes = oldCertificate.Export(X509ContentType.Pkcs12, certificatePassword);

            var newCertificate = new X509Certificate2(TestResources.TestCertificatePath, "testPassword", X509KeyStorageFlags.Exportable);
            var newCertificateBytes = newCertificate.Export(X509ContentType.Pkcs12, certificatePassword);

            Directory.CreateDirectory(Path.GetDirectoryName(certificatePath));
            File.WriteAllBytes(certificatePath, oldCertificateBytes);

            var endpointConfigurationCallCount = 0;
            var config = new ConfigurationBuilder().AddInMemoryCollection(new[]
            {
                    new KeyValuePair<string, string>("Endpoints:End1:Url", "https://*:5001"),
                    new KeyValuePair<string, string>("Endpoints:End1:Certificate:Path", certificatePath),
                    new KeyValuePair<string, string>("Endpoints:End1:Certificate:Password", certificatePassword),
                }).Build();

            var configLoader = serverOptions
                .Configure(config, reloadOnChange)
                .Endpoint("End1", opt =>
                {
                    Assert.True(opt.IsHttps);
                    var expectedSerialNumber = endpointConfigurationCallCount == 0
                        ? oldCertificate.SerialNumber
                        : newCertificate.SerialNumber;
                    Assert.Equal(opt.HttpsOptions.ServerCertificate.SerialNumber, expectedSerialNumber);
                    endpointConfigurationCallCount++;
                });

            configLoader.Load();

            var fileTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            if (reloadOnChange) // There's no reload token if !reloadOnChange
            {
                configLoader.GetReloadToken().RegisterChangeCallback(_ => fileTcs.SetResult(), state: null);
            }
            File.WriteAllBytes(certificatePath, newCertificateBytes);

            if (reloadOnChange)
            {
                await fileTcs.Task.TimeoutAfter(TimeSpan.FromSeconds(10)); // Needs to be meaningfully longer than the polling period - 4 seconds
            }
            else
            {
                // We can't just check immediately that the callback hasn't fired - we might preempt it
                await Task.Delay(TimeSpan.FromSeconds(1));
                Assert.False(fileTcs.Task.IsCompleted);
            }

            Assert.Equal(1, endpointConfigurationCallCount);

            if (reloadOnChange)
            {
                configLoader.Reload();

                Assert.Equal(2, endpointConfigurationCallCount);
            }
        }
        finally
        {
            if (File.Exists(certificatePath))
            {
                // Note: the watcher will see this event, but we ignore deletions, so it shouldn't matter
                File.Delete(certificatePath);
            }
        }
    }

    // On helix retry list - inherently flaky (FS events)
    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows)] // Windows has poor support for directory symlinks (e.g. https://github.com/dotnet/runtime/issues/27826)
    public async Task CertificateChangedOnDisk_Symlink()
    {
        var tempDir = Directory.CreateTempSubdirectory().FullName;

        try
        {
            // temp/
            //     tls.key -> link/tls.key
            //     link/ -> old/
            //     old/
            //         tls.key
            //     new/
            //         tls.key

            var oldDir = Directory.CreateDirectory(Path.Combine(tempDir, "old"));
            var newDir = Directory.CreateDirectory(Path.Combine(tempDir, "new"));
            var oldCertPath = Path.Combine(oldDir.FullName, "tls.key");
            var newCertPath = Path.Combine(newDir.FullName, "tls.key");

            var dirLink = Directory.CreateSymbolicLink(Path.Combine(tempDir, "link"), "./old");
            var fileLink = File.CreateSymbolicLink(Path.Combine(tempDir, "tls.key"), "./link/tls.key");

            var serverOptions = CreateServerOptions();

            var certificatePassword = "1234";

            var oldCertificate = new X509Certificate2(TestResources.GetCertPath("aspnetdevcert.pfx"), "testPassword", X509KeyStorageFlags.Exportable);
            var oldCertificateBytes = oldCertificate.Export(X509ContentType.Pkcs12, certificatePassword);

            File.WriteAllBytes(oldCertPath, oldCertificateBytes);

            var newCertificate = new X509Certificate2(TestResources.TestCertificatePath, "testPassword", X509KeyStorageFlags.Exportable);
            var newCertificateBytes = newCertificate.Export(X509ContentType.Pkcs12, certificatePassword);

            File.WriteAllBytes(newCertPath, newCertificateBytes);

            var endpointConfigurationCallCount = 0;
            var config = new ConfigurationBuilder().AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string>("Endpoints:End1:Url", "https://*:5001"),
                new KeyValuePair<string, string>("Endpoints:End1:Certificate:Path", fileLink.FullName),
                new KeyValuePair<string, string>("Endpoints:End1:Certificate:Password", certificatePassword),
            }).Build();

            var configLoader = serverOptions
                .Configure(config, reloadOnChange: true)
                .Endpoint("End1", opt =>
                {
                    Assert.True(opt.IsHttps);
                    var expectedSerialNumber = endpointConfigurationCallCount == 0
                        ? oldCertificate.SerialNumber
                        : newCertificate.SerialNumber;
                    Assert.Equal(opt.HttpsOptions.ServerCertificate.SerialNumber, expectedSerialNumber);
                    endpointConfigurationCallCount++;
                });

            configLoader.Load();

            var fileTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            configLoader.GetReloadToken().RegisterChangeCallback(_ => fileTcs.SetResult(), state: null);

            // Clobber link/ directory symlink - this will effectively cause the cert to be updated.
            // Unfortunately, it throws (file exists) if we don't delete the old one first so it's not a single, clean FS operation.
            dirLink.Delete();
            dirLink = Directory.CreateSymbolicLink(Path.Combine(tempDir, "link"), "./new");

            // This can fail in local runs where the timeout is 5 seconds and polling period is 4 seconds - just re-run
            await fileTcs.Task.DefaultTimeout();

            Assert.Equal(1, endpointConfigurationCallCount);

            configLoader.Reload();

            Assert.Equal(2, endpointConfigurationCallCount);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                // Note: the watcher will see this event, but we ignore deletions, so it shouldn't matter
                Directory.Delete(tempDir, recursive: true);
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
    // [InlineData("http1AndHttp2andHttp3", HttpProtocols.Http1AndHttp2AndHttp3)] // HTTP/3 not currently supported on macOS
    [MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win81)]
    public void DefaultConfigSectionCanSetProtocols_NonWin7(string input, HttpProtocols expected)
        => DefaultConfigSectionCanSetProtocols(input, expected);

    [ConditionalTheory]
    [InlineData("http1", HttpProtocols.Http1)]
    [InlineData("http2", HttpProtocols.Http2)]
    [InlineData("http1AndHttp2", HttpProtocols.Http1AndHttp2)]
    [InlineData("http1AndHttp2andHttp3", HttpProtocols.Http1AndHttp2AndHttp3)]
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
            Assert.Equal(ListenOptions.DefaultHttpProtocols, opt.Protocols);
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
                Assert.Equal(ListenOptions.DefaultHttpProtocols, opt.Protocols);
                ran2 = true;
            })
            .Load();
        serverOptions.ListenAnyIP(0, opt =>
        {
            // Kestrel default.
            Assert.Equal(ListenOptions.DefaultHttpProtocols, opt.Protocols);
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
#pragma warning disable SYSLIB0039 // TLS 1.0 and 1.1 are obsolete
                Assert.Equal(SslProtocols.Tls11, opt.HttpsOptions.SslProtocols);
#pragma warning restore SYSLIB0039
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
#pragma warning disable SYSLIB0039 // TLS 1.0 and 1.1 are obsolete
                Assert.Equal(SslProtocols.Tls11, opt.HttpsOptions.SslProtocols);
#pragma warning restore SYSLIB0039
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
#pragma warning disable SYSLIB0039 // TLS 1.0 and 1.1 are obsolete
                Assert.Equal(SslProtocols.Tls11, opt.HttpsOptions.SslProtocols);
#pragma warning restore SYSLIB0039
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

#pragma warning disable SYSLIB0039 // TLS 1.0 and 1.1 are obsolete
            Assert.Equal(SslProtocols.Tls11, opt.SslProtocols);
#pragma warning restore SYSLIB0039
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
    public void EndpointConfigureSection_CanSetClientCertificateMode()
    {
        var serverOptions = CreateServerOptions();
        var ranDefault = false;

        serverOptions.ConfigureHttpsDefaults(opt =>
        {
            opt.ServerCertificate = TestResources.GetTestCertificate();

            // Kestrel default
            Assert.Equal(ClientCertificateMode.NoCertificate, opt.ClientCertificateMode);
            ranDefault = true;
        });

        var ran1 = false;
        var ran2 = false;
        var config = new ConfigurationBuilder().AddInMemoryCollection(new[]
        {
            new KeyValuePair<string, string>("Endpoints:End1:ClientCertificateMode", "AllowCertificate"),
            new KeyValuePair<string, string>("Endpoints:End1:Url", "https://*:5001"),
        }).Build();
        serverOptions.Configure(config)
            .Endpoint("End1", opt =>
            {
                Assert.Equal(ClientCertificateMode.AllowCertificate, opt.HttpsOptions.ClientCertificateMode);
                ran1 = true;
            })
            .Load();
        serverOptions.ListenAnyIP(0, opt =>
        {
            opt.UseHttps(httpsOptions =>
            {
                // Kestrel default.
                Assert.Equal(ClientCertificateMode.NoCertificate, httpsOptions.ClientCertificateMode);
                ran2 = true;
            });
        });

        Assert.True(ranDefault);
        Assert.True(ran1);
        Assert.True(ran2);
    }

    [Fact]
    public void EndpointConfigureSection_CanConfigureSni()
    {
        var serverOptions = CreateServerOptions();
        var certPath = Path.Combine("shared", "TestCertificates", "https-ecdsa.pem");
        var keyPath = Path.Combine("shared", "TestCertificates", "https-ecdsa.key");

        var config = new ConfigurationBuilder().AddInMemoryCollection(new[]
        {
            new KeyValuePair<string, string>("Endpoints:End1:Url", "https://*:5001"),
            new KeyValuePair<string, string>("Endpoints:End1:Sni:*.example.org:Protocols", HttpProtocols.None.ToString()),
            new KeyValuePair<string, string>("Endpoints:End1:Sni:*.example.org:SslProtocols:0", SslProtocols.Tls13.ToString()),
            new KeyValuePair<string, string>("Endpoints:End1:Sni:*.example.org:ClientCertificateMode", ClientCertificateMode.RequireCertificate.ToString()),
            new KeyValuePair<string, string>("Endpoints:End1:Sni:*.example.org:Certificate:Path", certPath),
            new KeyValuePair<string, string>("Endpoints:End1:Sni:*.example.org:Certificate:KeyPath", keyPath),
        }).Build();

        var (_, endpointsToStart) = serverOptions.Configure(config).Reload();
        var end1 = Assert.Single(endpointsToStart);
        var (name, sniConfig) = Assert.Single(end1?.EndpointConfig?.Sni);

        Assert.Equal("*.example.org", name);
        Assert.Equal(HttpProtocols.None, sniConfig.Protocols);
        Assert.Equal(SslProtocols.Tls13, sniConfig.SslProtocols);
        Assert.Equal(ClientCertificateMode.RequireCertificate, sniConfig.ClientCertificateMode);
        Assert.Equal(certPath, sniConfig.Certificate.Path);
        Assert.Equal(keyPath, sniConfig.Certificate.KeyPath);
    }

    [Fact]
    public void EndpointConfigureSection_CanOverrideClientCertificateModeFromConfigureHttpsDefaults()
    {
        var serverOptions = CreateServerOptions();

        serverOptions.ConfigureHttpsDefaults(opt =>
        {
            opt.ServerCertificate = TestResources.GetTestCertificate();
            opt.ClientCertificateMode = ClientCertificateMode.RequireCertificate;
        });

        var ran1 = false;
        var config = new ConfigurationBuilder().AddInMemoryCollection(new[]
        {
                new KeyValuePair<string, string>("Endpoints:End1:ClientCertificateMode", "AllowCertificate"),
                new KeyValuePair<string, string>("Endpoints:End1:Url", "https://*:5001"),
            }).Build();
        serverOptions.Configure(config)
            .Endpoint("End1", opt =>
            {
                Assert.Equal(ClientCertificateMode.AllowCertificate, opt.HttpsOptions.ClientCertificateMode);
                ran1 = true;
            })
            .Load();

        Assert.True(ran1);
    }

    [Fact]
    public void DefaultEndpointConfigureSection_CanSetClientCertificateMode()
    {
        var serverOptions = CreateServerOptions();

        serverOptions.ConfigureHttpsDefaults(opt =>
        {
            opt.ServerCertificate = TestResources.GetTestCertificate();
        });

        var ran1 = false;
        var config = new ConfigurationBuilder().AddInMemoryCollection(new[]
        {
            new KeyValuePair<string, string>("EndpointDefaults:ClientCertificateMode", "AllowCertificate"),
            new KeyValuePair<string, string>("Endpoints:End1:Url", "https://*:5001"),
        }).Build();
        serverOptions.Configure(config)
            .Endpoint("End1", opt =>
            {
                Assert.Equal(ClientCertificateMode.AllowCertificate, opt.HttpsOptions.ClientCertificateMode);
                ran1 = true;
            })
            .Load();

        Assert.True(ran1);
    }

    [Fact]
    public void DefaultEndpointConfigureSection_ConfigureHttpsDefaultsCanOverrideClientCertificateMode()
    {
        var serverOptions = CreateServerOptions();

        serverOptions.ConfigureHttpsDefaults(opt =>
        {
            opt.ServerCertificate = TestResources.GetTestCertificate();

            Assert.Equal(ClientCertificateMode.AllowCertificate, opt.ClientCertificateMode);
            opt.ClientCertificateMode = ClientCertificateMode.RequireCertificate;
        });

        var ran1 = false;
        var config = new ConfigurationBuilder().AddInMemoryCollection(new[]
        {
            new KeyValuePair<string, string>("EndpointDefaults:ClientCertificateMode", "AllowCertificate"),
            new KeyValuePair<string, string>("Endpoints:End1:Url", "https://*:5001"),
        }).Build();
        serverOptions.Configure(config)
            .Endpoint("End1", opt =>
            {
                Assert.Equal(ClientCertificateMode.RequireCertificate, opt.HttpsOptions.ClientCertificateMode);
                ran1 = true;
            })
            .Load();

        Assert.True(ran1);
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

        // NonDefaultProtocol is unchanged and doesn't need to be stopped/started
        var stopEndpoint = Assert.Single(endpointsToStop);
        var startEndpoint = Assert.Single(endpointsToStart);

        Assert.Equal(5000, stopEndpoint.IPEndPoint.Port);
        Assert.Equal(ListenOptions.DefaultHttpProtocols, stopEndpoint.Protocols);

        Assert.Equal(5000, startEndpoint.IPEndPoint.Port);
        Assert.Equal(HttpProtocols.Http1, startEndpoint.Protocols);
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

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public void MultipleLoads_Consecutive(bool loadInternal, bool reloadOnChange)
    {
        var serverOptions = CreateServerOptions();
        var mockConfig = CreateMockConfiguration();
        serverOptions.Configure(mockConfig.Object, reloadOnChange);

        Action load = loadInternal ? serverOptions.ConfigurationLoader.LoadInternal : serverOptions.ConfigurationLoader.Load;

        load();

        mockConfig.Verify(c => c.GetSection(It.IsAny<string>()), Times.AtLeastOnce);

        mockConfig.Invocations.Clear();

        load();

        // In any case, nothing has changed, so nothing is read
        mockConfig.Verify(c => c.GetSection(It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public void MultipleLoads_ConfigureBetween(bool loadInternal, bool reloadOnChange)
    {
        var serverOptions = CreateServerOptions();
        var mockConfig = CreateMockConfiguration();
        serverOptions.Configure(mockConfig.Object, reloadOnChange);

        var oldConfigurationLoader = serverOptions.ConfigurationLoader;

        if (loadInternal)
        {
            serverOptions.ConfigurationLoader.LoadInternal();
        }
        else
        {
            serverOptions.ConfigurationLoader.Load();
        }

        mockConfig.Verify(c => c.GetSection(It.IsAny<string>()), Times.AtLeastOnce);

        mockConfig.Invocations.Clear();

        serverOptions.Configure(mockConfig.Object, reloadOnChange: false);
        var newConfigurationLoader = serverOptions.ConfigurationLoader;
        Assert.NotSame(oldConfigurationLoader, newConfigurationLoader);

        if (loadInternal)
        {
            serverOptions.ConfigurationLoader.LoadInternal();
        }
        else
        {
            serverOptions.ConfigurationLoader.Load();
        }

        // In any case, the configuration loader has been replaced, so this is a "first" load
        mockConfig.Verify(c => c.GetSection(It.IsAny<string>()), Times.AtLeastOnce);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public void MultipleLoadInternals_ConfigurationChanges(bool loadInternal, bool reloadOnChange)
    {
        var serverOptions = CreateServerOptions();
        var mockConfig = CreateMockConfiguration(out var mockReloadToken);
        serverOptions.Configure(mockConfig.Object, reloadOnChange);

        Action load = loadInternal ? serverOptions.ConfigurationLoader.LoadInternal : serverOptions.ConfigurationLoader.Load;

        load();

        mockReloadToken.VerifyGet(t => t.HasChanged, Times.Never);
        mockConfig.Verify(c => c.GetSection(It.IsAny<string>()), Times.AtLeastOnce);

        mockReloadToken.SetupGet(t => t.HasChanged).Returns(true);

        mockReloadToken.Invocations.Clear();
        mockConfig.Invocations.Clear();

        load();

        Func<Times> reloadTimes = loadInternal && reloadOnChange ? Times.AtLeastOnce : Times.Never;

        mockReloadToken.VerifyGet(t => t.HasChanged, reloadTimes);
        mockConfig.Verify(c => c.GetSection(It.IsAny<string>()), reloadTimes);
    }

    [Fact]
    public void LoadInternalBeforeLoad()
    {
        var serverOptions = CreateServerOptions();
        var mockConfig = CreateMockConfiguration(out var mockReloadToken);
        serverOptions.Configure(mockConfig.Object, reloadOnChange: true);

        serverOptions.ConfigurationLoader.LoadInternal();

        mockReloadToken.VerifyGet(t => t.HasChanged, Times.Never);
        mockConfig.Verify(c => c.GetSection(It.IsAny<string>()), Times.AtLeastOnce);

        mockReloadToken.SetupGet(t => t.HasChanged).Returns(true);

        mockReloadToken.Invocations.Clear();
        mockConfig.Invocations.Clear();

        serverOptions.ConfigurationLoader.LocalhostEndpoint(5000);

        serverOptions.ConfigurationLoader.Load();

        mockReloadToken.VerifyGet(t => t.HasChanged, Times.Never);
        mockConfig.Verify(c => c.GetSection(It.IsAny<string>()), Times.Never);
        Assert.Single(serverOptions.CodeBackedListenOptions); // Still have to process endpoints
    }

    [Fact]
    public void LoadInternalAfterLoad()
    {
        var serverOptions = CreateServerOptions();
        var mockConfig = CreateMockConfiguration(out var mockReloadToken);
        serverOptions.Configure(mockConfig.Object, reloadOnChange: true);

        serverOptions.ConfigurationLoader.Load();

        mockReloadToken.VerifyGet(t => t.HasChanged, Times.Never);
        mockConfig.Verify(c => c.GetSection(It.IsAny<string>()), Times.AtLeastOnce);

        mockReloadToken.SetupGet(t => t.HasChanged).Returns(true);

        mockReloadToken.Invocations.Clear();
        mockConfig.Invocations.Clear();

        serverOptions.ConfigurationLoader.LoadInternal();

        mockReloadToken.VerifyGet(t => t.HasChanged, Times.AtLeastOnce);
        mockConfig.Verify(c => c.GetSection(It.IsAny<string>()), Times.AtLeastOnce);
    }

    [Fact]
    public void ProcessEndpointsToAdd()
    {
        int numEndpointsToAdd = 3;
        int numEndpointsAdded = 0;

        var serverOptions = CreateServerOptions();
        serverOptions.Configure();

        for (int i = 0; i < numEndpointsToAdd; i++)
        {
            serverOptions.ConfigurationLoader.LocalhostEndpoint(5000 + i, _ => numEndpointsAdded++);
        }

        serverOptions.ConfigurationLoader.ProcessEndpointsToAdd();

        Assert.Equal(numEndpointsToAdd, numEndpointsAdded);
        Assert.Equal(numEndpointsToAdd, serverOptions.CodeBackedListenOptions.Count);
        Assert.Empty(serverOptions.ConfigurationBackedListenOptions);

        // Adding more endpoints and calling again has no effect

        for (int i = 0; i < numEndpointsToAdd; i++)
        {
            serverOptions.ConfigurationLoader.LocalhostEndpoint(6000 + i, _ => numEndpointsAdded++);
        }

        serverOptions.ConfigurationLoader.ProcessEndpointsToAdd();

        Assert.Equal(numEndpointsToAdd, numEndpointsAdded);
        Assert.Equal(numEndpointsToAdd, serverOptions.CodeBackedListenOptions.Count);
        Assert.Empty(serverOptions.ConfigurationBackedListenOptions);
    }

    [Fact]
    public void ProcessEndpointsToAdd_CallbackThrows()
    {
        int numEndpointsAdded = 0;

        var serverOptions = CreateServerOptions();
        serverOptions.Configure();

        serverOptions.ConfigurationLoader.LocalhostEndpoint(5000, _ => numEndpointsAdded++);
        serverOptions.ConfigurationLoader.LocalhostEndpoint(5001, _ => throw new InvalidOperationException());
        serverOptions.ConfigurationLoader.LocalhostEndpoint(5002, _ => numEndpointsAdded++);

        Assert.Throws<InvalidOperationException>(serverOptions.ConfigurationLoader.ProcessEndpointsToAdd);

        Assert.Equal(1, numEndpointsAdded);
        Assert.Single(serverOptions.CodeBackedListenOptions);
        Assert.Empty(serverOptions.ConfigurationBackedListenOptions);

        serverOptions.ConfigurationLoader.ProcessEndpointsToAdd();

        // As in success scenarios, the second call has no effect
        Assert.Equal(1, numEndpointsAdded);
        Assert.Single(serverOptions.CodeBackedListenOptions);
        Assert.Empty(serverOptions.ConfigurationBackedListenOptions);
    }

    [Fact]
    public void ProcessEndpointsToAddBeforeLoad()
    {
        var serverOptions = CreateServerOptions();
        var mockConfig = CreateMockConfiguration();
        serverOptions.Configure(mockConfig.Object);

        serverOptions.ConfigurationLoader.LocalhostEndpoint(5000);

        serverOptions.ConfigurationLoader.ProcessEndpointsToAdd();

        Assert.Single(serverOptions.CodeBackedListenOptions);
        mockConfig.Verify(c => c.GetSection(It.IsNotIn("EndpointDefaults")), Times.Never); // It does read the EndpointDefaults sections

        mockConfig.Invocations.Clear();

        serverOptions.ConfigurationLoader.LocalhostEndpoint(7000, _ => Assert.Fail("New endpoints should not be added after ProcessEndpointsToAdd"));

        serverOptions.ConfigurationLoader.Load();

        Assert.Single(serverOptions.CodeBackedListenOptions);
        mockConfig.Verify(c => c.GetSection(It.IsAny<string>()), Times.AtLeastOnce); // Still need to load, even if endpoints have been processed
    }

    [Fact]
    public void ProcessEndpointsToAddAfterLoad()
    {
        var serverOptions = CreateServerOptions();
        var mockConfig = CreateMockConfiguration();
        serverOptions.Configure(mockConfig.Object);

        serverOptions.ConfigurationLoader.LocalhostEndpoint(5000);

        serverOptions.ConfigurationLoader.Load();

        Assert.Single(serverOptions.CodeBackedListenOptions);

        mockConfig.Invocations.Clear();

        serverOptions.ConfigurationLoader.LocalhostEndpoint(7000, _ => Assert.Fail("New endpoints should not be added after Load"));

        Assert.Single(serverOptions.CodeBackedListenOptions);
        serverOptions.ConfigurationLoader.ProcessEndpointsToAdd();
    }

    [Fact]
    public void LoadInternalDoesNotAddEndpoints()
    {
        var serverOptions = CreateServerOptions();
        serverOptions.Configure();

        serverOptions.ConfigurationLoader.LocalhostEndpoint(7000, _ => Assert.Fail("New endpoints should not be added by LoadInternal"));

        serverOptions.ConfigurationLoader.LoadInternal();
    }

    [Fact]
    public void ReloadDoesNotAddEndpoints()
    {
        var serverOptions = CreateServerOptions();
        serverOptions.Configure();

        serverOptions.ConfigurationLoader.Load();

        serverOptions.ConfigurationLoader.LocalhostEndpoint(7000, _ => Assert.Fail("New endpoints should not be added by Reload"));

        _ = serverOptions.ConfigurationLoader.Reload();
    }

    [Fact]
    public void AddNamedPipeEndpoint()
    {
        var serverOptions = CreateServerOptions();
        var builder = serverOptions.Configure()
            .NamedPipeEndpoint("abc");

        Assert.Empty(serverOptions.GetListenOptions());
        Assert.Equal(builder, serverOptions.ConfigurationLoader);

        builder.Load();

        Assert.Single(serverOptions.GetListenOptions());
        Assert.Equal("abc", serverOptions.CodeBackedListenOptions[0].PipeName);
        Assert.NotNull(serverOptions.ConfigurationLoader);
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

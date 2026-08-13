// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.DirectTls.Tests;

public class DirectTlsTransportFactoryTests
{
    private sealed class TrackingMemoryPoolFactory : IMemoryPoolFactory<byte>
    {
        public TrackingMemoryPool Pool { get; } = new();
        public MemoryPoolOptions Options { get; private set; }
        public int CreateCount { get; private set; }

        public MemoryPool<byte> Create(MemoryPoolOptions options = null)
        {
            CreateCount++;
            Options = options;
            return Pool;
        }
    }

    private sealed class TrackingMemoryPool : MemoryPool<byte>
    {
        public bool IsDisposed { get; private set; }

        public override int MaxBufferSize => MemoryPool<byte>.Shared.MaxBufferSize;

        public override IMemoryOwner<byte> Rent(int minBufferSize = -1)
            => MemoryPool<byte>.Shared.Rent(minBufferSize);

        protected override void Dispose(bool disposing)
        {
            IsDisposed = true;
        }
    }

    private static DirectTlsTransportFactory CreateFactory(DirectTlsTransportOptions options = null)
    {
        return new DirectTlsTransportFactory(
            Options.Create(options ?? new DirectTlsTransportOptions()),
            NullLoggerFactory.Instance);
    }

    [Fact]
    public void CanBind_DirectTlsEndpoint_ReturnsTrue()
    {
        var factory = CreateFactory();

        Assert.True(factory.CanBind(new DirectTlsEndpoint(IPAddress.Loopback, 0)));
    }

    [Fact]
    public void CanBind_PlainIPEndPoint_ReturnsFalse()
    {
        var factory = CreateFactory();

        Assert.False(factory.CanBind(new IPEndPoint(IPAddress.Loopback, 0)));
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX)]
    public async Task BindAsync_NonDirectTlsEndpoint_Throws()
    {
        var factory = CreateFactory();

        await Assert.ThrowsAsync<NotSupportedException>(
            () => factory.BindAsync(new IPEndPoint(IPAddress.Loopback, 0)).AsTask());
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX)]
    public async Task BindAsync_WithoutServerCertificate_Throws()
    {
        var factory = CreateFactory();
        var endpoint = new DirectTlsEndpoint(IPAddress.Loopback, 0);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => factory.BindAsync(endpoint).AsTask());
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX)]
    public async Task BindAsync_DelayCertificateMode_Throws()
    {
        var factory = CreateFactory();
        var endpoint = new DirectTlsEndpoint(IPAddress.Loopback, 0)
        {
            Options =
            {
                ServerCertificate = TestResources.GetTestCertificate(),
                ClientCertificateMode = ClientCertificateMode.DelayCertificate,
            },
        };

        await Assert.ThrowsAsync<NotSupportedException>(
            () => factory.BindAsync(endpoint).AsTask());
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX)]
    public void UseDirectTls_UsesMemoryPoolFactoryFromDependencyInjection()
    {
        var memoryPoolFactory = new TrackingMemoryPoolFactory();

        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder.UseKestrel().UseDirectTls();
                webHostBuilder.ConfigureServices(services =>
                    services.AddSingleton<IMemoryPoolFactory<byte>>(memoryPoolFactory));
                webHostBuilder.Configure(_ => { });
            })
            .Build();

        var options = host.Services.GetRequiredService<IOptions<DirectTlsTransportOptions>>().Value;

        Assert.Same(memoryPoolFactory, options.MemoryPoolFactory);
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX)]
    public async Task BindAsync_UsesConfiguredMemoryPoolFactoryAndDisposesPoolWithListener()
    {
        var memoryPoolFactory = new TrackingMemoryPoolFactory();
        var factory = CreateFactory(new DirectTlsTransportOptions
        {
            WorkerCount = 1,
            MemoryPoolFactory = memoryPoolFactory,
        });
        var endpoint = new DirectTlsEndpoint(IPAddress.Loopback, 0)
        {
            Options =
            {
                ServerCertificate = TestResources.GetTestCertificate(),
            },
        };

        var listener = await factory.BindAsync(endpoint);

        try
        {
            Assert.Equal(1, memoryPoolFactory.CreateCount);
            Assert.Equal("kestrel", memoryPoolFactory.Options?.Owner);
            Assert.False(memoryPoolFactory.Pool.IsDisposed);
        }
        finally
        {
            await listener.DisposeAsync();
        }

        Assert.True(memoryPoolFactory.Pool.IsDisposed);
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX)]
    public async Task BindAsync_WhenBindFails_RollsBackListenerResources()
    {
        // Occupy an ephemeral port with a first listener, then bind a second listener to that exact port. The
        // second Bind() fails with AddressInUse AFTER the pump pool, TLS contexts, and memory pool are already
        // allocated - so the factory must roll the listener back (dispose it) rather than leak those resources.
        var firstFactory = CreateFactory(new DirectTlsTransportOptions { WorkerCount = 1 });
        var firstEndpoint = new DirectTlsEndpoint(IPAddress.Loopback, 0)
        {
            Options = { ServerCertificate = TestResources.GetTestCertificate() },
        };

        var firstListener = await firstFactory.BindAsync(firstEndpoint);
        try
        {
            var boundPort = ((IPEndPoint)firstListener.EndPoint).Port;

            var memoryPoolFactory = new TrackingMemoryPoolFactory();
            var secondFactory = CreateFactory(new DirectTlsTransportOptions
            {
                WorkerCount = 1,
                MemoryPoolFactory = memoryPoolFactory,
            });
            var secondEndpoint = new DirectTlsEndpoint(IPAddress.Loopback, boundPort)
            {
                Options = { ServerCertificate = TestResources.GetTestCertificate() },
            };

            await Assert.ThrowsAsync<AddressInUseException>(() => secondFactory.BindAsync(secondEndpoint).AsTask());

            // The rolled-back listener owns the memory pool it allocated; a proper rollback disposes it.
            Assert.True(memoryPoolFactory.Pool.IsDisposed);
        }
        finally
        {
            await firstListener.DisposeAsync();
        }
    }

    [Fact]
    public void BuildClientCertificateValidation_DisposesConvertedCertificate()
    {
        using var sourceCertificate = TestResources.GetTestCertificate();
        using var legacyCertificate = new X509Certificate(sourceCertificate.Export(X509ContentType.Cert));
        X509Certificate2 convertedCertificate = null;
        var endpointOptions = new DirectTlsEndpointOptions
        {
            ClientCertificateMode = ClientCertificateMode.AllowCertificate,
            ClientCertificateValidation = (certificate, _, _) =>
            {
                convertedCertificate = certificate;
                Assert.NotEqual(IntPtr.Zero, certificate.Handle);
                return true;
            },
        };
        var validation = DirectTlsTransportFactory.BuildClientCertificateValidation(endpointOptions);

        Assert.True(validation(new object(), legacyCertificate, chain: null, SslPolicyErrors.None));
        Assert.NotNull(convertedCertificate);
        Assert.Equal(IntPtr.Zero, convertedCertificate.Handle);
    }

    [Fact]
    public void BuildClientCertificateValidation_DoesNotDisposeProvidedCertificate2()
    {
        using var certificate = TestResources.GetTestCertificate();
        var endpointOptions = new DirectTlsEndpointOptions
        {
            ClientCertificateMode = ClientCertificateMode.AllowCertificate,
            ClientCertificateValidation = (_, _, _) => true,
        };
        var validation = DirectTlsTransportFactory.BuildClientCertificateValidation(endpointOptions);

        Assert.True(validation(new object(), certificate, chain: null, SslPolicyErrors.None));
        Assert.NotEqual(IntPtr.Zero, certificate.Handle);
    }
}

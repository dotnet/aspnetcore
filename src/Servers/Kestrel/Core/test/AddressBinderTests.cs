// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests;

public class AddressBinderTests
{
    private readonly Func<ListenOptions, ListenOptions> _noopUseHttps = l => l;

    [Theory]
    [InlineData("http://10.10.10.10:5000/", "10.10.10.10", 5000)]
    [InlineData("http://[::1]:5000", "::1", 5000)]
    [InlineData("http://[::1]", "::1", 80)]
    [InlineData("http://127.0.0.1", "127.0.0.1", 80)]
    [InlineData("https://127.0.0.1", "127.0.0.1", 443)]
    public void CorrectIPEndpointsAreCreated(string address, string expectedAddress, int expectedPort)
    {
        Assert.True(AddressBinder.TryCreateIPEndPoint(
            BindingAddress.Parse(address), out var endpoint));
        Assert.NotNull(endpoint);
        Assert.Equal(IPAddress.Parse(expectedAddress), endpoint.Address);
        Assert.Equal(expectedPort, endpoint.Port);
    }

    [Theory]
    [InlineData("http://*")]
    [InlineData("http://*:5000")]
    [InlineData("http://+:80")]
    [InlineData("http://+")]
    [InlineData("http://randomhost:6000")]
    [InlineData("http://randomhost")]
    [InlineData("https://randomhost")]
    public void DoesNotCreateIPEndPointOnInvalidIPAddress(string address)
    {
        Assert.False(AddressBinder.TryCreateIPEndPoint(
            BindingAddress.Parse(address), out var endpoint));
    }

    [Theory]
    [InlineData("*")]
    [InlineData("randomhost")]
    [InlineData("+")]
    [InlineData("contoso.com")]
    public void ParseAddressDefaultsToAnyIPOnInvalidIPAddress(string host)
    {
        var listenOptions = AddressBinder.ParseAddress($"http://{host}", out var https);
        Assert.IsType<AnyIPListenOptions>(listenOptions);
        Assert.IsType<IPEndPoint>(listenOptions.EndPoint);
        Assert.Equal(IPAddress.IPv6Any, listenOptions.IPEndPoint.Address);
        Assert.Equal(80, listenOptions.IPEndPoint.Port);
        Assert.False(https);
    }

    [Fact]
    public void ParseAddressLocalhost()
    {
        var listenOptions = AddressBinder.ParseAddress("http://localhost", out var https);
        Assert.IsType<LocalhostListenOptions>(listenOptions);
        Assert.IsType<IPEndPoint>(listenOptions.EndPoint);
        Assert.Equal(IPAddress.Loopback, listenOptions.IPEndPoint.Address);
        Assert.Equal(80, listenOptions.IPEndPoint.Port);
        Assert.False(https);
    }

    [Fact]
    public void ParseAddress_HasPipeNoSlash()
    {
        // Pipe prefix is missing slash here and so the address is parsed as an IP.
        // The slash is required to differentiate between a pipe and a hostname.
        var listenOptions = AddressBinder.ParseAddress("http://pipe:8080", out var https);
        Assert.IsType<IPEndPoint>(listenOptions.EndPoint);
        Assert.Equal(8080, listenOptions.IPEndPoint.Port);
        Assert.False(https);
    }

    [Fact]
    public void ParseAddressNamedPipe()
    {
        var address = "http://pipe:/HelloWorld";
        var listenOptions = AddressBinder.ParseAddress(address, out var https);
        Assert.IsType<NamedPipeEndPoint>(listenOptions.EndPoint);
        Assert.Equal("HelloWorld", listenOptions.PipeName);
        Assert.False(https);
        Assert.Equal(address, listenOptions.GetDisplayName());
    }

    [Fact]
    public void ParseAddressNamedPipe_BackSlashes()
    {
        var address = @"http://pipe:/LOCAL\HelloWorld";
        var listenOptions = AddressBinder.ParseAddress(address, out var https);
        Assert.IsType<NamedPipeEndPoint>(listenOptions.EndPoint);
        Assert.Equal(@"LOCAL\HelloWorld", listenOptions.PipeName);
        Assert.False(https);
        Assert.Equal(address, listenOptions.GetDisplayName());
    }

    [Fact]
    public void ParseAddressNamedPipe_ForwardSlashes()
    {
        var address = "http://pipe://tmp/kestrel-test.sock";
        var listenOptions = AddressBinder.ParseAddress(address, out var https);
        Assert.IsType<NamedPipeEndPoint>(listenOptions.EndPoint);
        Assert.Equal("/tmp/kestrel-test.sock", listenOptions.PipeName);
        Assert.False(https);
        Assert.Equal(address, listenOptions.GetDisplayName());
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows, SkipReason = "tmp/kestrel-test.sock is not valid for windows. Unix socket path must be absolute.")]
    public void ParseAddressUnixPipe()
    {
        var listenOptions = AddressBinder.ParseAddress("http://unix:/tmp/kestrel-test.sock", out var https);
        Assert.IsType<UnixDomainSocketEndPoint>(listenOptions.EndPoint);
        Assert.Equal("/tmp/kestrel-test.sock", listenOptions.SocketPath);
        Assert.False(https);
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX, SkipReason = "Windows has drive letters and volume separator (c:), testing this url on unix or osx provides completely different output.")]
    [MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win10_RS4)]
    public void ParseAddressUnixPipeOnWindows()
    {
        var listenOptions = AddressBinder.ParseAddress(@"http://unix:/c:/foo/bar/pipe.socket", out var https);
        Assert.IsType<UnixDomainSocketEndPoint>(listenOptions.EndPoint);
        Assert.Equal("c:/foo/bar/pipe.socket", listenOptions.SocketPath);
        Assert.False(https);
    }

    [Theory]
    [InlineData("http://10.10.10.10:5000/", "10.10.10.10", 5000, false)]
    [InlineData("http://[::1]:5000", "::1", 5000, false)]
    [InlineData("http://[::1]", "::1", 80, false)]
    [InlineData("http://127.0.0.1", "127.0.0.1", 80, false)]
    [InlineData("https://127.0.0.1", "127.0.0.1", 443, true)]
    public void ParseAddressIP(string address, string ip, int port, bool isHttps)
    {
        var listenOptions = AddressBinder.ParseAddress(address, out var https);
        Assert.IsType<IPEndPoint>(listenOptions.EndPoint);
        Assert.Equal(IPAddress.Parse(ip), listenOptions.IPEndPoint.Address);
        Assert.Equal(port, listenOptions.IPEndPoint.Port);
        Assert.Equal(isHttps, https);
    }

    [Fact]
    public async Task WrapsAddressInUseExceptionAsIOException()
    {
        var addresses = new ServerAddressesFeature();
        addresses.InternalCollection.Add("http://localhost:5000");
        var options = new KestrelServerOptions();

        var addressBindContext = TestContextFactory.CreateAddressBindContext(
            addresses,
            options,
            NullLogger.Instance,
            endpoint => throw new AddressInUseException("already in use"));

        await Assert.ThrowsAsync<IOException>(() =>
            AddressBinder.BindAsync(options.GetListenOptions(), addressBindContext, _noopUseHttps, CancellationToken.None));
    }

    [Fact]
    public void LogsWarningWhenHostingAddressesAreOverridden()
    {
        var logger = new TestApplicationErrorLogger();

        var overriddenAddress = "http://localhost:5000";
        var addresses = new ServerAddressesFeature();
        addresses.InternalCollection.Add(overriddenAddress);

        var options = new KestrelServerOptions();
        options.ListenAnyIP(8080);

        var addressBindContext = TestContextFactory.CreateAddressBindContext(
            addresses,
            options,
            logger,
            endpoint => Task.CompletedTask);

        var bindTask = AddressBinder.BindAsync(options.GetListenOptions(), addressBindContext, _noopUseHttps, CancellationToken.None);
        Assert.True(bindTask.IsCompletedSuccessfully);

        var log = Assert.Single(logger.Messages);
        Assert.Equal(LogLevel.Warning, log.LogLevel);
        Assert.Equal(CoreStrings.FormatOverridingWithKestrelOptions(overriddenAddress), log.Message);
    }

    [Fact]
    public void LogsInformationWhenKestrelAddressesAreOverridden()
    {
        var logger = new TestApplicationErrorLogger();

        var overriddenAddress = "http://localhost:5000";
        var addresses = new ServerAddressesFeature();
        addresses.InternalCollection.Add(overriddenAddress);

        var options = new KestrelServerOptions();
        options.ListenAnyIP(8080);

        var addressBindContext = TestContextFactory.CreateAddressBindContext(
            addresses,
            options,
            logger,
            endpoint => Task.CompletedTask);

        addressBindContext.ServerAddressesFeature.PreferHostingUrls = true;

        var bindTask = AddressBinder.BindAsync(options.GetListenOptions(), addressBindContext, _noopUseHttps, CancellationToken.None);
        Assert.True(bindTask.IsCompletedSuccessfully);

        var log = Assert.Single(logger.Messages);
        Assert.Equal(LogLevel.Information, log.LogLevel);
        Assert.Equal(CoreStrings.FormatOverridingWithPreferHostingUrls(nameof(addressBindContext.ServerAddressesFeature.PreferHostingUrls), overriddenAddress), log.Message);
    }

    [Fact]
    public async Task FlowsCancellationTokenToCreateBinddingCallback()
    {
        var addresses = new ServerAddressesFeature();
        addresses.InternalCollection.Add("http://localhost:5000");
        var options = new KestrelServerOptions();

        var addressBindContext = TestContextFactory.CreateAddressBindContext(
            addresses,
            options,
            NullLogger.Instance,
            (endpoint, cancellationToken) =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                return Task.CompletedTask;
            });

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            AddressBinder.BindAsync(options.GetListenOptions(), addressBindContext, _noopUseHttps, new CancellationToken(true)));
    }

    [Theory]
    [InlineData("http://*:80")]
    [InlineData("http://+:80")]
    [InlineData("http://contoso.com:80")]
    public async Task FallbackToIPv4WhenIPv6AnyBindFails(string address)
    {
        var logger = new MockLogger();
        var addresses = new ServerAddressesFeature();
        addresses.InternalCollection.Add(address);
        var options = new KestrelServerOptions();

        var ipV6Attempt = false;
        var ipV4Attempt = false;

        var addressBindContext = TestContextFactory.CreateAddressBindContext(
            addresses,
            options,
            logger,
            endpoint =>
            {
                if (endpoint.IPEndPoint.Address.Equals(IPAddress.IPv6Any))
                {
                    ipV6Attempt = true;
                    throw new InvalidOperationException("EAFNOSUPPORT");
                }

                if (endpoint.IPEndPoint.Address.Equals(IPAddress.Any))
                {
                    ipV4Attempt = true;
                }

                return Task.CompletedTask;
            });

        await AddressBinder.BindAsync(options.GetListenOptions(), addressBindContext, _noopUseHttps, CancellationToken.None);

        Assert.True(ipV4Attempt, "Should have attempted to bind to IPAddress.Any");
        Assert.True(ipV6Attempt, "Should have attempted to bind to IPAddress.IPv6Any");
        Assert.Contains(logger.Messages, f => f.Equals(CoreStrings.FormatFallbackToIPv4Any(80)));
    }

    [Fact]
    public async Task DefaultAddressBinderBindsToHttpPort5000()
    {
        var logger = new MockLogger();
        var addresses = new ServerAddressesFeature();
        var services = new ServiceCollection();
        services.AddLogging();
        var options = new KestrelServerOptions()
        {
            ApplicationServices = services.BuildServiceProvider()
        };

        var endpoints = new List<ListenOptions>();

        var addressBindContext = TestContextFactory.CreateAddressBindContext(
            addresses,
            options,
            logger,
            listenOptions =>
            {
                endpoints.Add(listenOptions);
                return Task.CompletedTask;
            });

        await AddressBinder.BindAsync(options.GetListenOptions(), addressBindContext, _noopUseHttps, CancellationToken.None);

        Assert.Contains(endpoints, e => e.IPEndPoint.Port == 5000 && !e.IsTls);
    }
}

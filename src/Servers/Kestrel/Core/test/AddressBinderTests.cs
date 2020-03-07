// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    public class AddressBinderTests
    {
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
            addresses.Addresses.Add("http://localhost:5000");
            var options = new KestrelServerOptions();

            await Assert.ThrowsAsync<IOException>(() =>
                AddressBinder.BindAsync(addresses,
                    options,
                    NullLogger.Instance,
                    endpoint => throw new AddressInUseException("already in use")));
        }

        [Theory]
        [InlineData("http://*:80")]
        [InlineData("http://+:80")]
        [InlineData("http://contoso.com:80")]
        public async Task FallbackToIPv4WhenIPv6AnyBindFails(string address)
        {
            var logger = new MockLogger();
            var addresses = new ServerAddressesFeature();
            addresses.Addresses.Add(address);
            var options = new KestrelServerOptions();

            var ipV6Attempt = false;
            var ipV4Attempt = false;

            await AddressBinder.BindAsync(addresses,
                options,
                logger,
                endpoint =>
                {
                    if (endpoint.IPEndPoint.Address == IPAddress.IPv6Any)
                    {
                        ipV6Attempt = true;
                        throw new InvalidOperationException("EAFNOSUPPORT");
                    }

                    if (endpoint.IPEndPoint.Address == IPAddress.Any)
                    {
                        ipV4Attempt = true;
                    }

                    return Task.CompletedTask;
                });

            Assert.True(ipV4Attempt, "Should have attempted to bind to IPAddress.Any");
            Assert.True(ipV6Attempt, "Should have attempted to bind to IPAddress.IPv6Any");
            Assert.Contains(logger.Messages, f => f.Equals(CoreStrings.FormatFallbackToIPv4Any(80)));
        }

        [Fact]
        public async Task DefaultAddressBinderWithoutDevCertButHttpsConfiguredBindsToHttpsPorts()
        {
            var x509Certificate2 = TestResources.GetTestCertificate();
            var logger = new MockLogger();
            var addresses = new ServerAddressesFeature();
            var services = new ServiceCollection();
            services.AddLogging();
            var options = new KestrelServerOptions()
            {
                // This stops the dev cert from being loaded
                IsDevCertLoaded = true,
                ApplicationServices = services.BuildServiceProvider()
            };

            options.ConfigureEndpointDefaults(e =>
            {
                if (e.IPEndPoint.Port == 5001)
                {
                    e.UseHttps(new HttpsConnectionAdapterOptions { ServerCertificate = x509Certificate2 });
                }
            });

            var endpoints = new List<ListenOptions>();
            await AddressBinder.BindAsync(addresses, options, logger, listenOptions =>
            {
                endpoints.Add(listenOptions);
                return Task.CompletedTask;
            });

            Assert.Contains(endpoints, e => e.IPEndPoint.Port == 5000 && !e.IsTls);
            Assert.Contains(endpoints, e => e.IPEndPoint.Port == 5001 && e.IsTls);
        }
    }
}

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal;
using Microsoft.AspNetCore.Testing;
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
                ServerAddress.FromUrl(address), out var endpoint));
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
                ServerAddress.FromUrl(address), out var endpoint));
        }

        [Theory]
        [InlineData("*")]
        [InlineData("randomhost")]
        [InlineData("+")]
        [InlineData("contoso.com")]
        public async Task DefaultsToIPv6AnyOnInvalidIPAddress(string host)
        {
            var addresses = new ServerAddressesFeature();
            addresses.Addresses.Add($"http://{host}");
            var options = new List<ListenOptions>();

            var tcs = new TaskCompletionSource<ListenOptions>();
            await AddressBinder.BindAsync(addresses,
                options,
                NullLogger.Instance,
                endpoint =>
                {
                    tcs.TrySetResult(endpoint);
                    return Task.CompletedTask;
                });
            var result = await tcs.Task;
            Assert.Equal(IPAddress.IPv6Any, result.IPEndPoint.Address);
        }

        [Fact]
        public async Task WrapsAddressInUseExceptionAsIOException()
        {
            var addresses = new ServerAddressesFeature();
            addresses.Addresses.Add("http://localhost:5000");
            var options = new List<ListenOptions>();

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
            var options = new List<ListenOptions>();

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
    }
}

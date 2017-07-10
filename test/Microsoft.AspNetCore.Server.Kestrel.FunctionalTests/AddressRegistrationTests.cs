// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Testing;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Server.Kestrel.FunctionalTests
{
    public class AddressRegistrationTests
    {
        private const int MaxRetries = 10;

        private readonly Action<ILoggingBuilder> _configureLoggingDelegate;

        public AddressRegistrationTests(ITestOutputHelper output)
        {
            _configureLoggingDelegate = builder => builder.AddXunit(output);
        }

        [ConditionalFact]
        [NetworkIsReachable]
        public async Task RegisterAddresses_HostName_Success()
        {
            var hostName = Dns.GetHostName();
            await RegisterAddresses_Success($"http://{hostName}:0", $"http://{hostName}");
        }

        [Theory]
        [MemberData(nameof(AddressRegistrationDataIPv4))]
        public async Task RegisterAddresses_IPv4_Success(string addressInput, string testUrl)
        {
            await RegisterAddresses_Success(addressInput, testUrl);
        }

        [ConditionalTheory]
        [MemberData(nameof(AddressRegistrationDataIPv4Port5000Default))]
        [PortSupportedCondition(5000)]
        public async Task RegisterAddresses_IPv4Port5000Default_Success(string addressInput, string testUrl)
        {
            await RegisterAddresses_Success(addressInput, testUrl, 5000);
        }

        [ConditionalTheory]
        [MemberData(nameof(AddressRegistrationDataIPv4Port80))]
        [PortSupportedCondition(80)]
        public async Task RegisterAddresses_IPv4Port80_Success(string addressInput, string testUrl)
        {
            await RegisterAddresses_Success(addressInput, testUrl, 80);
        }

        [Fact]
        public async Task RegisterAddresses_IPv4StaticPort_Success()
        {
            await RegisterAddresses_StaticPort_Success("http://127.0.0.1", "http://127.0.0.1");
        }

        [Fact]
        public async Task RegisterAddresses_IPv4LocalhostStaticPort_Success()
        {
            await RegisterAddresses_StaticPort_Success("http://localhost", "http://127.0.0.1");
        }

        [Fact]
        public async Task RegisterIPEndPoint_IPv4StaticPort_Success()
        {
            await RegisterIPEndPoint_StaticPort_Success(IPAddress.Loopback, $"http://127.0.0.1");
        }

        [ConditionalFact]
        [IPv6SupportedCondition]
        public async Task RegisterIPEndPoint_IPv6StaticPort_Success()
        {
            await RegisterIPEndPoint_StaticPort_Success(IPAddress.IPv6Loopback, $"http://[::1]");
        }

        [ConditionalTheory]
        [MemberData(nameof(IPEndPointRegistrationDataDynamicPort))]
        [IPv6SupportedCondition]
        public async Task RegisterIPEndPoint_DynamicPort_Success(IPEndPoint endPoint, string testUrl)
        {
            await RegisterIPEndPoint_Success(endPoint, testUrl);
        }

        [ConditionalTheory]
        [MemberData(nameof(IPEndPointRegistrationDataPort443))]
        [IPv6SupportedCondition]
        [PortSupportedCondition(443)]
        public async Task RegisterIPEndPoint_Port443_Success(IPEndPoint endpoint, string testUrl)
        {
            await RegisterIPEndPoint_Success(endpoint, testUrl, 443);
        }

        [ConditionalTheory]
        [MemberData(nameof(AddressRegistrationDataIPv6))]
        [IPv6SupportedCondition]
        public async Task RegisterAddresses_IPv6_Success(string addressInput, string[] testUrls)
        {
            await RegisterAddresses_Success(addressInput, testUrls);
        }

        [ConditionalTheory]
        [MemberData(nameof(AddressRegistrationDataIPv6Port5000Default))]
        [IPv6SupportedCondition]
        [PortSupportedCondition(5000)]
        public async Task RegisterAddresses_IPv6Port5000Default_Success(string addressInput, string[] testUrls)
        {
            await RegisterAddresses_Success(addressInput, testUrls);
        }

        [ConditionalTheory]
        [MemberData(nameof(AddressRegistrationDataIPv6Port80))]
        [IPv6SupportedCondition]
        [PortSupportedCondition(80)]
        public async Task RegisterAddresses_IPv6Port80_Success(string addressInput, string[] testUrls)
        {
            await RegisterAddresses_Success(addressInput, testUrls);
        }

        [ConditionalTheory]
        [MemberData(nameof(AddressRegistrationDataIPv6ScopeId))]
        [IPv6SupportedCondition]
        [IPv6ScopeIdPresentCondition]
        public async Task RegisterAddresses_IPv6ScopeId_Success(string addressInput, string testUrl)
        {
            await RegisterAddresses_Success(addressInput, testUrl);
        }

        [ConditionalFact]
        [IPv6SupportedCondition]
        public async Task RegisterAddresses_IPv6StaticPort_Success()
        {
            await RegisterAddresses_StaticPort_Success("http://[::1]", "http://[::1]");
        }

        [ConditionalFact]
        [IPv6SupportedCondition]
        public async Task RegisterAddresses_IPv6LocalhostStaticPort_Success()
        {
            await RegisterAddresses_StaticPort_Success("http://localhost", new[] { "http://localhost", "http://127.0.0.1", "http://[::1]" });
        }

        private async Task RegisterAddresses_Success(string addressInput, string[] testUrls, int testPort = 0)
        {
            var hostBuilder = new WebHostBuilder()
                .UseKestrel()
                .ConfigureLogging(_configureLoggingDelegate)
                .UseUrls(addressInput)
                .Configure(ConfigureEchoAddress);

            using (var host = hostBuilder.Build())
            {
                host.Start();

                foreach (var testUrl in testUrls.Select(testUrl => $"{testUrl}:{(testPort == 0 ? host.GetPort() : testPort)}"))
                {
                    var response = await HttpClientSlim.GetStringAsync(testUrl, validateCertificate: false);

                    // Compare the response with Uri.ToString(), rather than testUrl directly.
                    // Required to handle IPv6 addresses with zone index, like "fe80::3%1"
                    Assert.Equal(new Uri(testUrl).ToString(), response);
                }
            }
        }

        private Task RegisterAddresses_Success(string addressInput, string testUrl, int testPort = 0)
            => RegisterAddresses_Success(addressInput, new[] { testUrl }, testPort);

        private async Task RegisterAddresses_StaticPort_Success(string addressInput, string[] testUrls)
        {
            var retryCount = 0;
            var errors = new List<Exception>();

            while (retryCount < MaxRetries)
            {
                try
                {
                    var port = GetNextPort();
                    await RegisterAddresses_Success($"{addressInput}:{port}", testUrls, port);
                    return;
                }
                catch (XunitException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    errors.Add(ex);
                }

                retryCount++;
            }

            if (errors.Any())
            {
                throw new AggregateException(errors);
            }
        }

        private Task RegisterAddresses_StaticPort_Success(string addressInput, string testUrl)
            => RegisterAddresses_StaticPort_Success(addressInput, new[] { testUrl });

        private async Task RegisterIPEndPoint_Success(IPEndPoint endPoint, string testUrl, int testPort = 0)
        {
            var hostBuilder = new WebHostBuilder()
                .ConfigureLogging(_configureLoggingDelegate)
                .UseKestrel(options =>
                {
                    options.Listen(endPoint, listenOptions =>
                    {
                        if (testUrl.StartsWith("https"))
                        {
                            listenOptions.UseHttps(TestResources.TestCertificatePath, "testPassword");
                        }
                    });
                })
                .Configure(ConfigureEchoAddress);

            using (var host = hostBuilder.Build())
            {
                host.Start();

                var testUrlWithPort = $"{testUrl}:{(testPort == 0 ? host.GetPort() : testPort)}";

                var options = ((IOptions<KestrelServerOptions>)host.Services.GetService(typeof(IOptions<KestrelServerOptions>))).Value;
                Assert.Single(options.ListenOptions);

                var response = await HttpClientSlim.GetStringAsync(testUrlWithPort, validateCertificate: false);

                // Compare the response with Uri.ToString(), rather than testUrl directly.
                // Required to handle IPv6 addresses with zone index, like "fe80::3%1"
                Assert.Equal(new Uri(testUrlWithPort).ToString(), response);
            }
        }

        private async Task RegisterIPEndPoint_StaticPort_Success(IPAddress address, string testUrl)
        {
            var retryCount = 0;
            var errors = new List<Exception>();

            while (retryCount < MaxRetries)
            {
                try
                {
                    var port = GetNextPort();
                    await RegisterIPEndPoint_Success(new IPEndPoint(address, port), testUrl, port);
                    return;
                }
                catch (XunitException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    errors.Add(ex);
                }

                retryCount++;
            }

            if (errors.Any())
            {
                throw new AggregateException(errors);
            }
        }

        [ConditionalFact]
        [PortSupportedCondition(5000)]
        public Task DefaultsServerAddress_BindsToIPv4()
        {
            return RegisterDefaultServerAddresses_Success(new[] { "http://127.0.0.1:5000" });
        }

        [ConditionalFact]
        [IPv6SupportedCondition]
        [PortSupportedCondition(5000)]
        public Task DefaultsServerAddress_BindsToIPv6()
        {
            return RegisterDefaultServerAddresses_Success(new[] { "http://127.0.0.1:5000", "http://[::1]:5000" });
        }

        private async Task RegisterDefaultServerAddresses_Success(IEnumerable<string> addresses)
        {
            var testLogger = new TestApplicationErrorLogger();

            var hostBuilder = new WebHostBuilder()
                .ConfigureLogging(_configureLoggingDelegate)
                .UseKestrel()
                .ConfigureLogging(builder => builder
                    .AddProvider(new KestrelTestLoggerProvider(testLogger))
                    .SetMinimumLevel(LogLevel.Debug))
                .Configure(ConfigureEchoAddress);

            using (var host = hostBuilder.Build())
            {
                host.Start();

                Assert.Equal(5000, host.GetPort());
                Assert.Single(testLogger.Messages, log => log.LogLevel == LogLevel.Debug &&
                    string.Equals(CoreStrings.FormatBindingToDefaultAddress(Constants.DefaultServerAddress),
                    log.Message, StringComparison.Ordinal));

                foreach (var address in addresses)
                {
                    Assert.Equal(new Uri(address).ToString(), await HttpClientSlim.GetStringAsync(address));
                }
            }
        }

        [Fact]
        public void ThrowsWhenBindingToIPv4AddressInUse()
        {
            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                var port = ((IPEndPoint)socket.LocalEndPoint).Port;

                var hostBuilder = new WebHostBuilder()
                    .ConfigureLogging(_configureLoggingDelegate)
                    .UseKestrel()
                    .UseUrls($"http://127.0.0.1:{port}")
                    .Configure(ConfigureEchoAddress);

                using (var host = hostBuilder.Build())
                {
                    var exception = Assert.Throws<IOException>(() => host.Start());
                    Assert.Equal(CoreStrings.FormatEndpointAlreadyInUse($"http://127.0.0.1:{port}"), exception.Message);
                }
            }
        }

        [ConditionalFact]
        [IPv6SupportedCondition]
        public void ThrowsWhenBindingToIPv6AddressInUse()
        {
            using (var socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp))
            {
                socket.Bind(new IPEndPoint(IPAddress.IPv6Loopback, 0));
                var port = ((IPEndPoint)socket.LocalEndPoint).Port;

                var hostBuilder = new WebHostBuilder()
                    .ConfigureLogging(_configureLoggingDelegate)
                    .UseKestrel()
                    .UseUrls($"http://[::1]:{port}")
                    .Configure(ConfigureEchoAddress);

                using (var host = hostBuilder.Build())
                {
                    var exception = Assert.Throws<IOException>(() => host.Start());
                    Assert.Equal(CoreStrings.FormatEndpointAlreadyInUse($"http://[::1]:{port}"), exception.Message);
                }
            }
        }

        [Fact]
        public async Task OverrideDirectConfigurationWithIServerAddressesFeature_Succeeds()
        {
            var useUrlsAddress = $"http://127.0.0.1:0";
            var testLogger = new TestApplicationErrorLogger();
            var hostBuilder = new WebHostBuilder()
                .UseKestrel(options =>
                {
                    options.Listen(new IPEndPoint(IPAddress.Loopback, 0), listenOptions =>
                    {
                        listenOptions.UseHttps("testCert.pfx", "testPassword");
                    });
                })
               .UseUrls(useUrlsAddress)
               .PreferHostingUrls(true)
               .ConfigureLogging(builder => builder.AddProvider(new KestrelTestLoggerProvider(testLogger)))
               .Configure(ConfigureEchoAddress);

            using (var host = hostBuilder.Build())
            {
                host.Start();

                var port = host.GetPort();

                // If this isn't working properly, we'll get the HTTPS endpoint defined in UseKestrel
                // instead of the HTTP endpoint defined in UseUrls.
                var serverAddresses = host.ServerFeatures.Get<IServerAddressesFeature>().Addresses;
                Assert.Equal(1, serverAddresses.Count);
                var useUrlsAddressWithPort = $"http://127.0.0.1:{port}";
                Assert.Equal(serverAddresses.First(), useUrlsAddressWithPort);

                Assert.Single(testLogger.Messages, log => log.LogLevel == LogLevel.Information &&
                    string.Equals(CoreStrings.FormatOverridingWithPreferHostingUrls(nameof(IServerAddressesFeature.PreferHostingUrls), useUrlsAddress),
                    log.Message, StringComparison.Ordinal));

                Assert.Equal(new Uri(useUrlsAddressWithPort).ToString(), await HttpClientSlim.GetStringAsync(useUrlsAddressWithPort));
            }
        }

        [Fact]
        public async Task DoesNotOverrideDirectConfigurationWithIServerAddressesFeature_IfPreferHostingUrlsFalse()
        {
            var useUrlsAddress = $"http://127.0.0.1:0";
            var testLogger = new TestApplicationErrorLogger();
            var hostBuilder = new WebHostBuilder()
                .ConfigureLogging(_configureLoggingDelegate)
                .UseKestrel(options =>
                {
                    options.Listen(new IPEndPoint(IPAddress.Loopback, 0), listenOptions =>
                    {
                        listenOptions.UseHttps("testCert.pfx", "testPassword");
                    });
                })
                .UseUrls($"http://127.0.0.1:0")
                .PreferHostingUrls(false)
                .ConfigureLogging(builder => builder.AddProvider(new KestrelTestLoggerProvider(testLogger)))
                .Configure(ConfigureEchoAddress);

            using (var host = hostBuilder.Build())
            {
                host.Start();

                var port = host.GetPort();

                // If this isn't working properly, we'll get the HTTP endpoint defined in UseUrls
                // instead of the HTTPS endpoint defined in UseKestrel.
                var serverAddresses = host.ServerFeatures.Get<IServerAddressesFeature>().Addresses;
                Assert.Equal(1, serverAddresses.Count);
                var endPointAddress = $"https://127.0.0.1:{port}";
                Assert.Equal(serverAddresses.First(), endPointAddress);

                Assert.Single(testLogger.Messages, log => log.LogLevel == LogLevel.Warning &&
                    string.Equals(CoreStrings.FormatOverridingWithKestrelOptions(useUrlsAddress, "UseKestrel()"),
                    log.Message, StringComparison.Ordinal));

                Assert.Equal(new Uri(endPointAddress).ToString(), await HttpClientSlim.GetStringAsync(endPointAddress, validateCertificate: false));
            }
        }

        [Fact]
        public async Task DoesNotOverrideDirectConfigurationWithIServerAddressesFeature_IfAddressesEmpty()
        {
            var hostBuilder = new WebHostBuilder()
                .ConfigureLogging(_configureLoggingDelegate)
                .UseKestrel(options =>
                {
                    options.Listen(new IPEndPoint(IPAddress.Loopback, 0), listenOptions =>
                    {
                        listenOptions.UseHttps("testCert.pfx", "testPassword");
                    });
                })
                .PreferHostingUrls(true)
                .Configure(ConfigureEchoAddress);

            using (var host = hostBuilder.Build())
            {
                host.Start();

                var port = host.GetPort();

                // If this isn't working properly, we'll not get the HTTPS endpoint defined in UseKestrel.
                var serverAddresses = host.ServerFeatures.Get<IServerAddressesFeature>().Addresses;
                Assert.Equal(1, serverAddresses.Count);
                var endPointAddress = $"https://127.0.0.1:{port}";
                Assert.Equal(serverAddresses.First(), endPointAddress);

                Assert.Equal(new Uri(endPointAddress).ToString(), await HttpClientSlim.GetStringAsync(endPointAddress, validateCertificate: false));
            }
        }

        [Fact]
        public void ThrowsWhenBindingLocalhostToIPv4AddressInUse()
        {
            ThrowsWhenBindingLocalhostToAddressInUse(AddressFamily.InterNetwork, IPAddress.Loopback);
        }

        [ConditionalFact]
        [IPv6SupportedCondition]
        public void ThrowsWhenBindingLocalhostToIPv6AddressInUse()
        {
            ThrowsWhenBindingLocalhostToAddressInUse(AddressFamily.InterNetworkV6, IPAddress.IPv6Loopback);
        }

        [Fact]
        public void ThrowsWhenBindingLocalhostToDynamicPort()
        {
            var hostBuilder = new WebHostBuilder()
                .UseKestrel()
                .UseUrls("http://localhost:0")
                .Configure(ConfigureEchoAddress);

            using (var host = hostBuilder.Build())
            {
                Assert.Throws<InvalidOperationException>(() => host.Start());
            }
        }

        [Theory]
        [InlineData("https://localhost")]
        [InlineData("ftp://localhost")]
        public void ThrowsForUnsupportedAddressFromHosting(string addr)
        {
            var hostBuilder = new WebHostBuilder()
                .UseKestrel()
                .UseUrls(addr)
                .Configure(ConfigureEchoAddress);

            using (var host = hostBuilder.Build())
            {
                Assert.Throws<InvalidOperationException>(() => host.Start());
            }
        }

        private void ThrowsWhenBindingLocalhostToAddressInUse(AddressFamily addressFamily, IPAddress address)
        {
            using (var socket = new Socket(addressFamily, SocketType.Stream, ProtocolType.Tcp))
            {
                socket.Bind(new IPEndPoint(address, 0));
                var port = ((IPEndPoint)socket.LocalEndPoint).Port;

                var hostBuilder = new WebHostBuilder()
                    .ConfigureLogging(_configureLoggingDelegate)
                    .UseKestrel()
                    .UseUrls($"http://localhost:{port}")
                    .Configure(ConfigureEchoAddress);

                using (var host = hostBuilder.Build())
                {
                    var exception = Assert.Throws<IOException>(() => host.Start());
                    Assert.Equal(
                        CoreStrings.FormatEndpointAlreadyInUse($"http://{(addressFamily == AddressFamily.InterNetwork ? "127.0.0.1" : "[::1]")}:{port}"),
                        exception.Message);
                }
            }
        }

        public static TheoryData<string, string> AddressRegistrationDataIPv4
        {
            get
            {
                var dataset = new TheoryData<string, string>();

                // Loopback
                dataset.Add("http://127.0.0.1:0", "http://127.0.0.1");

                // Any
                dataset.Add("http://*:0/", "http://127.0.0.1");
                dataset.Add("http://+:0/", "http://127.0.0.1");

                // Non-loopback addresses
                var ipv4Addresses = GetIPAddresses()
                    .Where(ip => ip.AddressFamily == AddressFamily.InterNetwork)
                    .Where(ip => CanBindAndConnectToEndpoint(new IPEndPoint(ip, 0)));

                foreach (var ip in ipv4Addresses)
                {
                    dataset.Add($"http://{ip}:0/", $"http://{ip}");
                }

                return dataset;
            }
        }

        public static TheoryData<string, string> AddressRegistrationDataIPv4Port5000Default =>
            new TheoryData<string, string>
            {
                { null, "http://127.0.0.1:5000/" },
                { string.Empty, "http://127.0.0.1:5000/" }
            };

        public static TheoryData<IPEndPoint, string> IPEndPointRegistrationDataDynamicPort
        {
            get
            {
                var dataset = new TheoryData<IPEndPoint, string>();

                // Loopback
                dataset.Add(new IPEndPoint(IPAddress.Loopback, 0), "http://127.0.0.1");
                dataset.Add(new IPEndPoint(IPAddress.Loopback, 0), "https://127.0.0.1");

                // IPv6 loopback
                dataset.Add(new IPEndPoint(IPAddress.IPv6Loopback, 0), "http://[::1]");
                dataset.Add(new IPEndPoint(IPAddress.IPv6Loopback, 0), "https://[::1]");

                // Any
                dataset.Add(new IPEndPoint(IPAddress.Any, 0), "http://127.0.0.1");
                dataset.Add(new IPEndPoint(IPAddress.Any, 0), "https://127.0.0.1");

                // IPv6 Any
                dataset.Add(new IPEndPoint(IPAddress.IPv6Any, 0), "http://127.0.0.1");
                dataset.Add(new IPEndPoint(IPAddress.IPv6Any, 0), "http://[::1]");
                dataset.Add(new IPEndPoint(IPAddress.IPv6Any, 0), "https://127.0.0.1");
                dataset.Add(new IPEndPoint(IPAddress.IPv6Any, 0), "https://[::1]");

                // Non-loopback addresses
                var ipv4Addresses = GetIPAddresses()
                    .Where(ip => ip.AddressFamily == AddressFamily.InterNetwork)
                    .Where(ip => CanBindAndConnectToEndpoint(new IPEndPoint(ip, 0)));

                foreach (var ip in ipv4Addresses)
                {
                    dataset.Add(new IPEndPoint(ip, 0), $"http://{ip}");
                    dataset.Add(new IPEndPoint(ip, 0), $"https://{ip}");
                }

                var ipv6Addresses = GetIPAddresses()
                    .Where(ip => ip.AddressFamily == AddressFamily.InterNetworkV6)
                    .Where(ip => ip.ScopeId == 0)
                    .Where(ip => CanBindAndConnectToEndpoint(new IPEndPoint(ip, 0)));

                foreach (var ip in ipv6Addresses)
                {
                    dataset.Add(new IPEndPoint(ip, 0), $"http://[{ip}]");
                }

                return dataset;
            }
        }

        public static TheoryData<string, string> AddressRegistrationDataIPv4Port80 =>
            new TheoryData<string, string>
            {
                // Default port for HTTP (80)
                {  "http://127.0.0.1", "http://127.0.0.1" },
                { "http://localhost", "http://127.0.0.1" },
                { "http://*", "http://127.0.0.1" }
            };

        public static TheoryData<IPEndPoint, string> IPEndPointRegistrationDataPort443 =>
            new TheoryData<IPEndPoint, string>
            {

                { new IPEndPoint(IPAddress.Loopback, 443), "https://127.0.0.1" },
                { new IPEndPoint(IPAddress.IPv6Loopback, 443), "https://[::1]" },
                { new IPEndPoint(IPAddress.Any, 443), "https://127.0.0.1" },
                { new IPEndPoint(IPAddress.IPv6Any, 443), "https://[::1]" }
            };

        public static TheoryData<string, string[]> AddressRegistrationDataIPv6
        {
            get
            {
                var dataset = new TheoryData<string, string[]>();

                // Loopback
                dataset.Add($"http://[::1]:0/", new[] { $"http://[::1]" });

                // Any
                dataset.Add($"http://*:0/", new[] { $"http://127.0.0.1", $"http://[::1]" });
                dataset.Add($"http://+:0/", new[] { $"http://127.0.0.1", $"http://[::1]" });

                // Non-loopback addresses
                var ipv6Addresses = GetIPAddresses()
                    .Where(ip => !ip.Equals(IPAddress.IPv6Loopback))
                    .Where(ip => ip.AddressFamily == AddressFamily.InterNetworkV6)
                    .Where(ip => ip.ScopeId == 0)
                    .Where(ip => CanBindAndConnectToEndpoint(new IPEndPoint(ip, 0)));

                foreach (var ip in ipv6Addresses)
                {
                    dataset.Add($"http://[{ip}]:0/", new[] { $"http://[{ip}]" });
                }

                return dataset;
            }
        }

        public static TheoryData<string, string[]> AddressRegistrationDataIPv6Port5000Default =>
            new TheoryData<string, string[]>
            {
                { null, new[] { "http://127.0.0.1:5000/", "http://[::1]:5000/" } },
                { string.Empty, new[] { "http://127.0.0.1:5000/", "http://[::1]:5000/" } }
            };

        public static TheoryData<string, string[]> AddressRegistrationDataIPv6Port80 =>
            new TheoryData<string, string[]>
            {
                // Default port for HTTP (80)
                { "http://[::1]", new[] { "http://[::1]/" } },
                { "http://localhost", new[] { "http://127.0.0.1/", "http://[::1]/" } },
                { "http://*", new[] { "http://[::1]/" } }
            };

        public static TheoryData<string, string> AddressRegistrationDataIPv6ScopeId
        {
            get
            {
                var dataset = new TheoryData<string, string>();

                var ipv6Addresses = GetIPAddresses()
                    .Where(ip => ip.AddressFamily == AddressFamily.InterNetworkV6)
                    .Where(ip => ip.ScopeId != 0)
                    .Where(ip => CanBindAndConnectToEndpoint(new IPEndPoint(ip, 0)));

                foreach (var ip in ipv6Addresses)
                {
                    dataset.Add($"http://[{ip}]:0/", $"http://[{ip}]");
                }

                // There may be no addresses with scope IDs and we need at least one data item in the
                // collection, otherwise xUnit fails the test run because a theory has no data.
                dataset.Add("http://[::1]:0", "http://[::1]");

                return dataset;
            }
        }

        private static IEnumerable<IPAddress> GetIPAddresses()
        {
            return NetworkInterface.GetAllNetworkInterfaces()
                .Where(i => i.OperationalStatus == OperationalStatus.Up)
                .SelectMany(i => i.GetIPProperties().UnicastAddresses)
                .Select(a => a.Address);
        }

        private void ConfigureEchoAddress(IApplicationBuilder app)
        {
            app.Run(context =>
            {
                return context.Response.WriteAsync(context.Request.GetDisplayUrl());
            });
        }

        private static int GetNextPort()
        {
            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                // Let the OS assign the next available port. Unless we cycle through all ports
                // on a test run, the OS will always increment the port number when making these calls.
                // This prevents races in parallel test runs where a test is already bound to
                // a given port, and a new test is able to bind to the same port due to port
                // reuse being enabled by default by the OS.
                socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                return ((IPEndPoint)socket.LocalEndPoint).Port;
            }
        }

        private static bool CanBindAndConnectToEndpoint(IPEndPoint endPoint)
        {
            try
            {
                using (var serverSocket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp))
                {
                    serverSocket.Bind(endPoint);
                    serverSocket.Listen(1);

                    var socketArgs = new SocketAsyncEventArgs
                    {
                        RemoteEndPoint = serverSocket.LocalEndPoint
                    };

                    var mre = new ManualResetEventSlim();
                    socketArgs.Completed += (s, e) =>
                    {
                        mre.Set();
                        e.ConnectSocket?.Dispose();
                    };

                    // Connect can take a couple minutes to time out.
                    if (Socket.ConnectAsync(SocketType.Stream, ProtocolType.Tcp, socketArgs))
                    {
                        return mre.Wait(5000) && socketArgs.SocketError == SocketError.Success;
                    }
                    else
                    {
                        socketArgs.ConnectSocket?.Dispose();
                        return socketArgs.SocketError == SocketError.Success;
                    }
                }
            }
            catch (SocketException)
            {
                return false;
            }
        }

        [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
        private class PortSupportedConditionAttribute : Attribute, ITestCondition
        {
            private readonly int _port;
            private readonly Lazy<bool> _portSupported;

            public PortSupportedConditionAttribute(int port)
            {
                _port = port;
                _portSupported = new Lazy<bool>(CanBindToPort);
            }

            public bool IsMet => _portSupported.Value;

            public string SkipReason => $"Cannot bind to port {_port} on the host.";

            private bool CanBindToPort()
            {
                try
                {
                    using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                    {
                        socket.Bind(new IPEndPoint(IPAddress.Loopback, _port));
                        return true;
                    }
                }
                catch (SocketException)
                {
                    return false;
                }
            }
        }
    }
}

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Infrastructure;
using Microsoft.AspNetCore.Testing;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.FunctionalTests
{
    public class AddressRegistrationTests
    {
        [Theory, MemberData(nameof(AddressRegistrationDataIPv4))]
        public async Task RegisterAddresses_IPv4_Success(string addressInput, Func<IServerAddressesFeature, string[]> testUrls)
        {
            await RegisterAddresses_Success(addressInput, testUrls);
        }

        [ConditionalTheory, MemberData(nameof(AddressRegistrationDataIPv4Port80))]
        [PortSupportedCondition(80)]
        public async Task RegisterAddresses_IPv4Port80_Success(string addressInput, Func<IServerAddressesFeature, string[]> testUrls)
        {
            await RegisterAddresses_Success(addressInput, testUrls);
        }

        [ConditionalTheory, MemberData(nameof(IPEndPointRegistrationDataRandomPort))]
        [IPv6SupportedCondition]
        public async Task RegisterIPEndPoint_RandomPort_Success(IPEndPoint endPoint, Func<IPEndPoint, string> testUrl)
        {
            await RegisterIPEndPoint_Success(endPoint, testUrl);
        }

        [ConditionalTheory, MemberData(nameof(IPEndPointRegistrationDataPort443))]
        [IPv6SupportedCondition]
        [PortSupportedCondition(443)]
        public async Task RegisterIPEndPoint_Port443_Success(IPEndPoint endpoint, Func<IPEndPoint, string> testUrl)
        {
            await RegisterIPEndPoint_Success(endpoint, testUrl);
        }

        [ConditionalTheory, MemberData(nameof(AddressRegistrationDataIPv6))]
        [IPv6SupportedCondition]
        public async Task RegisterAddresses_IPv6_Success(string addressInput, Func<IServerAddressesFeature, string[]> testUrls)
        {
            await RegisterAddresses_Success(addressInput, testUrls);
        }

        [ConditionalTheory, MemberData(nameof(AddressRegistrationDataIPv6Port80))]
        [IPv6SupportedCondition]
        [PortSupportedCondition(80)]
        public async Task RegisterAddresses_IPv6Port80_Success(string addressInput, Func<IServerAddressesFeature, string[]> testUrls)
        {
            await RegisterAddresses_Success(addressInput, testUrls);
        }

        [ConditionalTheory, MemberData(nameof(AddressRegistrationDataIPv6ScopeId))]
        [IPv6SupportedCondition]
        [IPv6ScopeIdPresentCondition]
        public async Task RegisterAddresses_IPv6ScopeId_Success(string addressInput, Func<IServerAddressesFeature, string[]> testUrls)
        {
            await RegisterAddresses_Success(addressInput, testUrls);
        }

        private async Task RegisterAddresses_Success(string addressInput, Func<IServerAddressesFeature, string[]> testUrls)
        {
            var hostBuilder = new WebHostBuilder()
                .UseKestrel()
                .UseUrls(addressInput)
                .Configure(ConfigureEchoAddress);

            using (var host = hostBuilder.Build())
            {
                host.Start();

                foreach (var testUrl in testUrls(host.ServerFeatures.Get<IServerAddressesFeature>()))
                {
                    var response = await HttpClientSlim.GetStringAsync(testUrl, validateCertificate: false);

                    // Compare the response with Uri.ToString(), rather than testUrl directly.
                    // Required to handle IPv6 addresses with zone index, like "fe80::3%1"
                    Assert.Equal(new Uri(testUrl).ToString(), response);
                }
            }
        }

        private async Task RegisterIPEndPoint_Success(IPEndPoint endPoint, Func<IPEndPoint, string> testUrl)
        {
            var hostBuilder = new WebHostBuilder()
                .UseKestrel(options =>
                {
                    options.Listen(endPoint, listenOptions =>
                    {
                        if (testUrl(listenOptions.IPEndPoint).StartsWith("https"))
                        {
                            listenOptions.UseHttps(TestResources.TestCertificatePath, "testPassword");
                        }
                    });
                })
                .Configure(ConfigureEchoAddress);

            using (var host = hostBuilder.Build())
            {
                host.Start();

                var options = ((IOptions<KestrelServerOptions>)host.Services.GetService(typeof(IOptions<KestrelServerOptions>))).Value;
                Assert.Single(options.ListenOptions);
                var listenOptions = options.ListenOptions[0];

                var response = await HttpClientSlim.GetStringAsync(testUrl(listenOptions.IPEndPoint), validateCertificate: false);

                // Compare the response with Uri.ToString(), rather than testUrl directly.
                // Required to handle IPv6 addresses with zone index, like "fe80::3%1"
                Assert.Equal(new Uri(testUrl(listenOptions.IPEndPoint)).ToString(), response);
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
               .UseKestrel()
               .ConfigureServices(services =>
               {
                   services.AddSingleton<ILoggerFactory>(new KestrelTestLoggerFactory(testLogger));
               })
               .Configure(ConfigureEchoAddress);

            using (var host = hostBuilder.Build())
            {
                host.Start();

                Assert.Equal(5000, host.GetPort());
                Assert.Single(testLogger.Messages, log => log.LogLevel == LogLevel.Debug &&
                    string.Equals($"No listening endpoints were configured. Binding to {Constants.DefaultServerAddress} by default.",
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
                var port = GetNextPort();
                socket.Bind(new IPEndPoint(IPAddress.Loopback, port));

                var hostBuilder = new WebHostBuilder()
                    .UseKestrel()
                    .UseUrls($"http://127.0.0.1:{port}")
                    .Configure(ConfigureEchoAddress);

                using (var host = hostBuilder.Build())
                {
                    var exception = Assert.Throws<IOException>(() => host.Start());
                    Assert.Equal($"Failed to bind to address http://127.0.0.1:{port}: address already in use.", exception.Message);
                }
            }
        }

        [ConditionalFact]
        [IPv6SupportedCondition]
        public void ThrowsWhenBindingToIPv6AddressInUse()
        {
            using (var socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp))
            {
                var port = GetNextPort();
                socket.Bind(new IPEndPoint(IPAddress.IPv6Loopback, port));

                var hostBuilder = new WebHostBuilder()
                    .UseKestrel()
                    .UseUrls($"http://[::1]:{port}")
                    .Configure(ConfigureEchoAddress);

                using (var host = hostBuilder.Build())
                {
                    var exception = Assert.Throws<IOException>(() => host.Start());
                    Assert.Equal($"Failed to bind to address http://[::1]:{port}: address already in use.", exception.Message);
                }
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

        private void ThrowsWhenBindingLocalhostToAddressInUse(AddressFamily addressFamily, IPAddress address)
        {
            using (var socket = new Socket(addressFamily, SocketType.Stream, ProtocolType.Tcp))
            {
                var port = GetNextPort();
                socket.Bind(new IPEndPoint(address, port));

                var hostBuilder = new WebHostBuilder()
                    .UseKestrel()
                    .UseUrls($"http://localhost:{port}")
                    .Configure(ConfigureEchoAddress);

                using (var host = hostBuilder.Build())
                {
                    var exception = Assert.Throws<IOException>(() => host.Start());
                    Assert.Equal(
                        $"Failed to bind to address http://localhost:{port} on the {(addressFamily == AddressFamily.InterNetwork ? "IPv4" : "IPv6")} loopback interface: port already in use.",
                        exception.Message);
                }
            }
        }

        public static TheoryData<string, Func<IServerAddressesFeature, string[]>> AddressRegistrationDataIPv4
        {
            get
            {
                var dataset = new TheoryData<string, Func<IServerAddressesFeature, string[]>>();

                // Default host and port
                dataset.Add(null, _ => new[] { "http://127.0.0.1:5000/" });
                dataset.Add(string.Empty, _ => new[] { "http://127.0.0.1:5000/" });

                // Static ports
                var port = GetNextPort();

                // Loopback
                dataset.Add($"http://127.0.0.1:{port}", _ => new[] { $"http://127.0.0.1:{port}/" });

                // Localhost
                dataset.Add($"http://localhost:{port}", _ => new[] { $"http://localhost:{port}/", $"http://127.0.0.1:{port}/" });

                // Any
                dataset.Add($"http://*:{port}/", _ => new[] { $"http://127.0.0.1:{port}/" });
                dataset.Add($"http://+:{port}/", _ => new[] { $"http://127.0.0.1:{port}/" });

                // Path after port
                dataset.Add($"http://127.0.0.1:{port}/base/path", _ => new[] { $"http://127.0.0.1:{port}/base/path" });

                // Dynamic port and non-loopback addresses
                dataset.Add("http://127.0.0.1:0/", GetTestUrls);
                dataset.Add($"http://{Dns.GetHostName()}:0/", GetTestUrls);

                var ipv4Addresses = GetIPAddresses()
                    .Where(ip => ip.AddressFamily == AddressFamily.InterNetwork);
                foreach (var ip in ipv4Addresses)
                {
                    dataset.Add($"http://{ip}:0/", GetTestUrls);
                }

                return dataset;
            }
        }

        public static TheoryData<IPEndPoint, Func<IPEndPoint, string>> IPEndPointRegistrationDataRandomPort
        {
            get
            {
                var dataset = new TheoryData<IPEndPoint, Func<IPEndPoint, string>>();

                // Static port
                var port = GetNextPort();

                // Loopback
                dataset.Add(new IPEndPoint(IPAddress.Loopback, port), _ => $"http://127.0.0.1:{port}/");
                dataset.Add(new IPEndPoint(IPAddress.Loopback, port), _ => $"https://127.0.0.1:{port}/");

                // IPv6 loopback
                dataset.Add(new IPEndPoint(IPAddress.IPv6Loopback, port), _ => FixTestUrl($"http://[::1]:{port}/"));
                dataset.Add(new IPEndPoint(IPAddress.IPv6Loopback, port), _ => FixTestUrl($"https://[::1]:{port}/"));

                // Any
                dataset.Add(new IPEndPoint(IPAddress.Any, port), _ => $"http://127.0.0.1:{port}/");
                dataset.Add(new IPEndPoint(IPAddress.Any, port), _ => $"https://127.0.0.1:{port}/");

                // IPv6 Any
                dataset.Add(new IPEndPoint(IPAddress.IPv6Any, port), _ => $"http://127.0.0.1:{port}/");
                dataset.Add(new IPEndPoint(IPAddress.IPv6Any, port), _ => FixTestUrl($"http://[::1]:{port}/"));
                dataset.Add(new IPEndPoint(IPAddress.IPv6Any, port), _ => $"https://127.0.0.1:{port}/");
                dataset.Add(new IPEndPoint(IPAddress.IPv6Any, port), _ => FixTestUrl($"https://[::1]:{port}/"));

                // Dynamic port
                dataset.Add(new IPEndPoint(IPAddress.Loopback, 0), endPoint => $"http://127.0.0.1:{endPoint.Port}/");
                dataset.Add(new IPEndPoint(IPAddress.Loopback, 0), endPoint => $"https://127.0.0.1:{endPoint.Port}/");

                var ipv4Addresses = GetIPAddresses()
                    .Where(ip => ip.AddressFamily == AddressFamily.InterNetwork);
                foreach (var ip in ipv4Addresses)
                {
                    dataset.Add(new IPEndPoint(ip, 0), endPoint => FixTestUrl($"http://{endPoint}/"));
                    dataset.Add(new IPEndPoint(ip, 0), endPoint => FixTestUrl($"https://{endPoint}/"));
                }

                return dataset;
            }
        }

        public static TheoryData<string, Func<IServerAddressesFeature, string[]>> AddressRegistrationDataIPv4Port80
        {
            get
            {
                var dataset = new TheoryData<string, Func<IServerAddressesFeature, string[]>>();

                // Default port for HTTP (80)
                dataset.Add("http://127.0.0.1", _ => new[] { "http://127.0.0.1/" });
                dataset.Add("http://localhost", _ => new[] { "http://127.0.0.1/" });
                dataset.Add("http://*", _ => new[] { "http://127.0.0.1/" });

                return dataset;
            }
        }

        public static TheoryData<IPEndPoint, Func<IPEndPoint, string>> IPEndPointRegistrationDataPort443
        {
            get
            {
                var dataset = new TheoryData<IPEndPoint, Func<IPEndPoint, string>>();

                dataset.Add(new IPEndPoint(IPAddress.Loopback, 443), _ => "https://127.0.0.1/");
                dataset.Add(new IPEndPoint(IPAddress.IPv6Loopback, 443), _ => FixTestUrl("https://[::1]/"));
                dataset.Add(new IPEndPoint(IPAddress.Any, 443), _ => "https://127.0.0.1/");
                dataset.Add(new IPEndPoint(IPAddress.IPv6Any, 443), _ => FixTestUrl("https://[::1]/"));

                return dataset;
            }
        }

        public static TheoryData<string, Func<IServerAddressesFeature, string[]>> AddressRegistrationDataIPv6
        {
            get
            {
                var dataset = new TheoryData<string, Func<IServerAddressesFeature, string[]>>();

                // Default host and port
                dataset.Add(null, _ => new[] { "http://127.0.0.1:5000/", "http://[::1]:5000/" });
                dataset.Add(string.Empty, _ => new[] { "http://127.0.0.1:5000/", "http://[::1]:5000/" });

                // Static ports
                var port = GetNextPort();

                // Loopback
                dataset.Add($"http://[::1]:{port}/",
                    _ => new[] { $"http://[::1]:{port}/" });

                // Localhost
                dataset.Add($"http://localhost:{port}",
                    _ => new[] { $"http://localhost:{port}/", $"http://127.0.0.1:{port}/", $"http://[::1]:{port}/" });

                // Any
                dataset.Add($"http://*:{port}/",
                    _ => new[] { $"http://127.0.0.1:{port}/", $"http://[::1]:{port}/" });
                dataset.Add($"http://+:{port}/",
                    _ => new[] { $"http://127.0.0.1:{port}/", $"http://[::1]:{port}/" });

                // Explicit IPv4 and IPv6 on same port
                dataset.Add($"http://127.0.0.1:{port}/;http://[::1]:{port}/",
                    _ => new[] { $"http://127.0.0.1:{port}/", $"http://[::1]:{port}/" });

                // Path after port
                dataset.Add($"http://[::1]:{port}/base/path",
                    _ => new[] { $"http://[::1]:{port}/base/path" });

                // Dynamic port and non-loopback addresses
                var ipv6Addresses = GetIPAddresses()
                    .Where(ip => ip.AddressFamily == AddressFamily.InterNetworkV6)
                    .Where(ip => ip.ScopeId == 0);
                foreach (var ip in ipv6Addresses)
                {
                    dataset.Add($"http://[{ip}]:0/", GetTestUrls);
                }

                return dataset;
            }
        }

        public static TheoryData<string, Func<IServerAddressesFeature, string[]>> AddressRegistrationDataIPv6Port80
        {
            get
            {
                var dataset = new TheoryData<string, Func<IServerAddressesFeature, string[]>>();

                // Default port for HTTP (80)
                dataset.Add("http://[::1]", _ => new[] { "http://[::1]/" });
                dataset.Add("http://localhost", _ => new[] { "http://127.0.0.1/", "http://[::1]/" });
                dataset.Add("http://*", _ => new[] { "http://[::1]/" });

                return dataset;
            }
        }

        public static TheoryData<string, Func<IServerAddressesFeature, string[]>> AddressRegistrationDataIPv6ScopeId
        {
            get
            {
                var dataset = new TheoryData<string, Func<IServerAddressesFeature, string[]>>();

                // Dynamic port
                var ipv6Addresses = GetIPAddresses()
                    .Where(ip => ip.AddressFamily == AddressFamily.InterNetworkV6)
                    .Where(ip => ip.ScopeId != 0);
                foreach (var ip in ipv6Addresses)
                {
                    dataset.Add($"http://[{ip}]:0/", GetTestUrls);
                }

                // There may be no addresses with scope IDs and we need at least one data item in the
                // collection, otherwise xUnit fails the test run because a theory has no data.
                dataset.Add("http://[::1]:0", GetTestUrls);

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

        private static string[] GetTestUrls(IServerAddressesFeature addressesFeature)
        {
            return addressesFeature.Addresses
                .Select(FixTestUrl)
                .ToArray();
        }

        private static string FixTestUrl(string url)
        {
            var fixedUrl = url.Replace("://+", "://localhost")
                .Replace("0.0.0.0", Dns.GetHostName())
                .Replace("[::]", Dns.GetHostName());

            if (!fixedUrl.EndsWith("/"))
            {
                fixedUrl = fixedUrl + "/";
            }

            return fixedUrl;
        }

        private void ConfigureEchoAddress(IApplicationBuilder app)
        {
            app.Run(context =>
            {
                return context.Response.WriteAsync(context.Request.GetDisplayUrl());
            });
        }

        private static int _nextPort = 8001;
        private static object _portLock = new object();
        private static int GetNextPort()
        {
            lock (_portLock)
            {
                using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    while (true)
                    {
                        try
                        {
                            var port = _nextPort++;
                            socket.Bind(new IPEndPoint(IPAddress.Loopback, port));
                            return port;
                        }
                        catch (SocketException)
                        {
                            // Retry unless exhausted
                            if (_nextPort == 65536)
                            {
                                throw;
                            }
                        }
                    }
                }
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

            public bool IsMet
            {
                get
                {
                    return _portSupported.Value;
                }
            }

            public string SkipReason
            {
                get
                {
                    return $"Cannot bind to port {_port} on the host.";
                }
            }

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

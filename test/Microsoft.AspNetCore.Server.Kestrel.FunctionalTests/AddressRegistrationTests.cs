// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
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
using Microsoft.AspNetCore.Server.Kestrel.Internal.Networking;
using Microsoft.AspNetCore.Testing;
using Microsoft.AspNetCore.Testing.xunit;
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

        [ConditionalTheory, MemberData(nameof(AddressRegistrationDataIPv4Port443))]
        [PortSupportedCondition(443)]
        public async Task RegisterAddresses_IPv4Port443_Success(string addressInput, Func<IServerAddressesFeature, string[]> testUrls)
        {
            await RegisterAddresses_Success(addressInput, testUrls);
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

        [ConditionalTheory, MemberData(nameof(AddressRegistrationDataIPv6Port443))]
        [IPv6SupportedCondition]
        [PortSupportedCondition(443)]
        public async Task RegisterAddresses_IPv6Port443_Success(string addressInput, Func<IServerAddressesFeature, string[]> testUrls)
        {
            await RegisterAddresses_Success(addressInput, testUrls);
        }

        [ConditionalTheory, MemberData(nameof(AddressRegistrationDataIPv6ScopeId))]
        [IPv6SupportedCondition]
        public async Task RegisterAddresses_IPv6ScopeId_Success(string addressInput, Func<IServerAddressesFeature, string[]> testUrls)
        {
            await RegisterAddresses_Success(addressInput, testUrls);
        }

        private async Task RegisterAddresses_Success(string addressInput, Func<IServerAddressesFeature, string[]> testUrls)
        {
            var hostBuilder = new WebHostBuilder()
                .UseKestrel(options =>
                {
                    options.UseHttps(@"TestResources/testCert.pfx", "testPassword");
                })
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
                    var exception = Assert.Throws<AggregateException>(() => host.Start());
                    Assert.Contains(exception.InnerExceptions, ex => ex is UvException);
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
                var port1 = GetNextPort();
                var port2 = GetNextPort();

                // Loopback
                dataset.Add($"http://127.0.0.1:{port1};https://127.0.0.1:{port2}",
                    _ => new[] { $"http://127.0.0.1:{port1}/", $"https://127.0.0.1:{port2}/" });

                // Localhost
                dataset.Add($"http://localhost:{port1};https://localhost:{port2}",
                    _ => new[] { $"http://localhost:{port1}/", $"http://127.0.0.1:{port1}/",
                                 $"https://localhost:{port2}/", $"https://127.0.0.1:{port2}/" });

                // Any
                dataset.Add($"http://*:{port1}/;https://*:{port2}/",
                    _ => new[] { $"http://127.0.0.1:{port1}/", $"https://127.0.0.1:{port2}/" });
                dataset.Add($"http://+:{port1}/;https://+:{port2}/",
                    _ => new[] { $"http://127.0.0.1:{port1}/", $"https://127.0.0.1:{port2}/" });

                // Path after port
                dataset.Add($"http://127.0.0.1:{port1}/base/path;https://127.0.0.1:{port2}/base/path",
                    _ => new[] { $"http://127.0.0.1:{port1}/base/path", $"https://127.0.0.1:{port2}/base/path" });

                // Dynamic port and non-loopback addresses
                dataset.Add("http://127.0.0.1:0/;https://127.0.0.1:0/", GetTestUrls);
                dataset.Add($"http://{Dns.GetHostName()}:0/;https://{Dns.GetHostName()}:0/", GetTestUrls);

                var ipv4Addresses = GetIPAddresses()
                    .Where(ip => ip.AddressFamily == AddressFamily.InterNetwork);
                foreach (var ip in ipv4Addresses)
                {
                    dataset.Add($"http://{ip}:0/;https://{ip}:0/", GetTestUrls);
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

        public static TheoryData<string, Func<IServerAddressesFeature, string[]>> AddressRegistrationDataIPv4Port443
        {
            get
            {
                var dataset = new TheoryData<string, Func<IServerAddressesFeature, string[]>>();

                // Default port for HTTPS (443)
                dataset.Add("https://127.0.0.1", _ => new[] { "https://127.0.0.1/" });
                dataset.Add("https://localhost", _ => new[] { "https://127.0.0.1/" });
                dataset.Add("https://*", _ => new[] { "https://127.0.0.1/" });

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
                var port1 = GetNextPort();
                var port2 = GetNextPort();

                // Loopback
                dataset.Add($"http://[::1]:{port1}/;https://[::1]:{port2}/",
                    _ => new[] { $"http://[::1]:{port1}/", $"https://[::1]:{port2}/" });

                // Localhost
                dataset.Add($"http://localhost:{port1};https://localhost:{port2}",
                    _ => new[] { $"http://localhost:{port1}/", $"http://127.0.0.1:{port1}/", $"http://[::1]:{port1}/",
                                 $"https://localhost:{port2}/", $"https://127.0.0.1:{port2}/", $"https://[::1]:{port2}/" });

                // Any
                dataset.Add($"http://*:{port1}/;https://*:{port2}/",
                    _ => new[] { $"http://127.0.0.1:{port1}/", $"http://[::1]:{port1}/",
                                 $"https://127.0.0.1:{port2}/", $"https://[::1]:{port2}/" });
                dataset.Add($"http://+:{port1}/;https://+:{port2}/",
                    _ => new[] { $"http://127.0.0.1:{port1}/", $"http://[::1]:{port1}/",
                                 $"https://127.0.0.1:{port2}/", $"https://[::1]:{port2}/" });

                // Explicit IPv4 and IPv6 on same port
                dataset.Add($"http://127.0.0.1:{port1}/;http://[::1]:{port1}/;https://127.0.0.1:{port2}/;https://[::1]:{port2}/",
                    _ => new[] { $"http://127.0.0.1:{port1}/", $"http://[::1]:{port1}/",
                                 $"https://127.0.0.1:{port2}/", $"https://[::1]:{port2}/" });

                // Path after port
                dataset.Add($"http://[::1]:{port1}/base/path;https://[::1]:{port2}/base/path",
                    _ => new[] { $"http://[::1]:{port1}/base/path", $"https://[::1]:{port2}/base/path" });

                // Dynamic port and non-loopback addresses
                var ipv6Addresses = GetIPAddresses()
                    .Where(ip => ip.AddressFamily == AddressFamily.InterNetworkV6)
                    .Where(ip => ip.ScopeId == 0);
                foreach (var ip in ipv6Addresses)
                {
                    dataset.Add($"http://[{ip}]:0/;https://[{ip}]:0/", GetTestUrls);
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

        public static TheoryData<string, Func<IServerAddressesFeature, string[]>> AddressRegistrationDataIPv6Port443
        {
            get
            {
                var dataset = new TheoryData<string, Func<IServerAddressesFeature, string[]>>();

                // Default port for HTTPS (443)
                dataset.Add("https://[::1]", _ => new[] { "https://[::1]/" });
                dataset.Add("https://localhost", _ => new[] { "https://127.0.0.1/", "https://[::1]/" });
                dataset.Add("https://*", _ => new[] { "https://[::1]/" });

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
                    dataset.Add($"http://[{ip}]:0/;https://[{ip}]:0/", GetTestUrls);
                }

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
                .Select(a => a.Replace("://+", "://localhost"))
                .Select(a => a.EndsWith("/") ? a : a + "/")
                .ToArray();
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

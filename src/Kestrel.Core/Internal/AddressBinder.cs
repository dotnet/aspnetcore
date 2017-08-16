// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Protocols;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal
{
    internal class AddressBinder
    {
        public static async Task BindAsync(IServerAddressesFeature addresses,
            List<ListenOptions> listenOptions,
            ILogger logger,
            Func<ListenOptions, Task> createBinding)
        {
            var strategy = CreateStrategy(
                listenOptions.ToArray(),
                addresses.Addresses.ToArray(),
                addresses.PreferHostingUrls);

            var context = new AddressBindContext
            {
                Addresses = addresses.Addresses,
                ListenOptions = listenOptions,
                Logger = logger,
                CreateBinding = createBinding
            };

            // reset options. The actual used options and addresses will be populated
            // by the address binding feature
            listenOptions.Clear();
            addresses.Addresses.Clear();

            await strategy.BindAsync(context).ConfigureAwait(false);
        }

        private class AddressBindContext
        {
            public ICollection<string> Addresses { get; set; }
            public List<ListenOptions> ListenOptions { get; set; }
            public ILogger Logger { get; set; }

            public Func<ListenOptions, Task> CreateBinding { get; set; }
        }

        private static IStrategy CreateStrategy(ListenOptions[] listenOptions, string[] addresses, bool preferAddresses)
        {
            var hasListenOptions = listenOptions.Length > 0;
            var hasAddresses = addresses.Length > 0;

            if (preferAddresses && hasAddresses)
            {
                if (hasListenOptions)
                {
                    return new OverrideWithAddressesStrategy(addresses);
                }

                return new AddressesStrategy(addresses);
            }
            else if (hasListenOptions)
            {
                if (hasAddresses)
                {
                    return new OverrideWithEndpointsStrategy(listenOptions, addresses);
                }

                return new EndpointsStrategy(listenOptions);
            }
            else if (hasAddresses)
            {
                // If no endpoints are configured directly using KestrelServerOptions, use those configured via the IServerAddressesFeature.
                return new AddressesStrategy(addresses);
            }
            else
            {
                // "localhost" for both IPv4 and IPv6 can't be represented as an IPEndPoint.
                return new DefaultAddressStrategy();
            }
        }

        /// <summary>
        /// Returns an <see cref="IPEndPoint"/> for the given host an port.
        /// If the host parameter isn't "localhost" or an IP address, use IPAddress.Any.
        /// </summary>
        protected internal static bool TryCreateIPEndPoint(ServerAddress address, out IPEndPoint endpoint)
        {
            if (!IPAddress.TryParse(address.Host, out var ip))
            {
                endpoint = null;
                return false;
            }

            endpoint = new IPEndPoint(ip, address.Port);
            return true;
        }

        private static Task BindEndpointAsync(IPEndPoint endpoint, AddressBindContext context)
            => BindEndpointAsync(new ListenOptions(endpoint), context);

        private static async Task BindEndpointAsync(ListenOptions endpoint, AddressBindContext context)
        {
            try
            {
                await context.CreateBinding(endpoint).ConfigureAwait(false);
            }
            catch (AddressInUseException ex)
            {
                throw new IOException(CoreStrings.FormatEndpointAlreadyInUse(endpoint), ex);
            }

            context.ListenOptions.Add(endpoint);
        }

        private static async Task BindLocalhostAsync(ServerAddress address, AddressBindContext context)
        {
            if (address.Port == 0)
            {
                throw new InvalidOperationException(CoreStrings.DynamicPortOnLocalhostNotSupported);
            }

            var exceptions = new List<Exception>();

            try
            {
                await BindEndpointAsync(new IPEndPoint(IPAddress.Loopback, address.Port), context).ConfigureAwait(false);
            }
            catch (Exception ex) when (!(ex is IOException))
            {
                context.Logger.LogWarning(0, CoreStrings.NetworkInterfaceBindingFailed, address, "IPv4 loopback", ex.Message);
                exceptions.Add(ex);
            }

            try
            {
                await BindEndpointAsync(new IPEndPoint(IPAddress.IPv6Loopback, address.Port), context).ConfigureAwait(false);
            }
            catch (Exception ex) when (!(ex is IOException))
            {
                context.Logger.LogWarning(0, CoreStrings.NetworkInterfaceBindingFailed, address, "IPv6 loopback", ex.Message);
                exceptions.Add(ex);
            }

            if (exceptions.Count == 2)
            {
                throw new IOException(CoreStrings.FormatAddressBindingFailed(address), new AggregateException(exceptions));
            }

            // If StartLocalhost doesn't throw, there is at least one listener.
            // The port cannot change for "localhost".
            context.Addresses.Add(address.ToString());
        }

        private static async Task BindAddressAsync(string address, AddressBindContext context)
        {
            var parsedAddress = ServerAddress.FromUrl(address);

            if (parsedAddress.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(CoreStrings.FormatConfigureHttpsFromMethodCall($"{nameof(KestrelServerOptions)}.{nameof(KestrelServerOptions.Listen)}()"));
            }
            else if (!parsedAddress.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(CoreStrings.FormatUnsupportedAddressScheme(address));
            }

            if (!string.IsNullOrEmpty(parsedAddress.PathBase))
            {
                throw new InvalidOperationException(CoreStrings.FormatConfigurePathBaseFromMethodCall($"{nameof(IApplicationBuilder)}.UsePathBase()"));
            }

            if (parsedAddress.IsUnixPipe)
            {
                var endPoint = new ListenOptions(parsedAddress.UnixPipePath);
                await BindEndpointAsync(endPoint, context).ConfigureAwait(false);
                context.Addresses.Add(endPoint.GetDisplayName());
            }
            else if (string.Equals(parsedAddress.Host, "localhost", StringComparison.OrdinalIgnoreCase))
            {
                // "localhost" for both IPv4 and IPv6 can't be represented as an IPEndPoint.
                await BindLocalhostAsync(parsedAddress, context).ConfigureAwait(false);
            }
            else
            {
                ListenOptions options;
                if (TryCreateIPEndPoint(parsedAddress, out var endpoint))
                {
                    options = new ListenOptions(endpoint);
                    await BindEndpointAsync(options, context).ConfigureAwait(false);
                }
                else
                {
                    // when address is 'http://hostname:port', 'http://*:port', or 'http://+:port'
                    try
                    {
                        options = new ListenOptions(new IPEndPoint(IPAddress.IPv6Any, parsedAddress.Port));
                        await BindEndpointAsync(options, context).ConfigureAwait(false);
                    }
                    catch (Exception ex) when (!(ex is IOException))
                    {
                        context.Logger.LogDebug(CoreStrings.FormatFallbackToIPv4Any(parsedAddress.Port));

                        // for machines that do not support IPv6
                        options = new ListenOptions(new IPEndPoint(IPAddress.Any, parsedAddress.Port));
                        await BindEndpointAsync(options, context).ConfigureAwait(false);
                    }
                }

                context.Addresses.Add(options.GetDisplayName());
            }
        }

        private interface IStrategy
        {
            Task BindAsync(AddressBindContext context);
        }

        private class DefaultAddressStrategy : IStrategy
        {
            public async Task BindAsync(AddressBindContext context)
            {
                context.Logger.LogDebug(CoreStrings.BindingToDefaultAddress, Constants.DefaultServerAddress);

                await BindLocalhostAsync(ServerAddress.FromUrl(Constants.DefaultServerAddress), context).ConfigureAwait(false);
            }
        }

        private class OverrideWithAddressesStrategy : AddressesStrategy
        {
            public OverrideWithAddressesStrategy(IReadOnlyCollection<string> addresses)
                : base(addresses)
            {
            }

            public override Task BindAsync(AddressBindContext context)
            {
                var joined = string.Join(", ", _addresses);
                context.Logger.LogInformation(CoreStrings.OverridingWithPreferHostingUrls, nameof(IServerAddressesFeature.PreferHostingUrls), joined);

                return base.BindAsync(context);
            }
        }

        private class OverrideWithEndpointsStrategy : EndpointsStrategy
        {
            private readonly string[] _originalAddresses;

            public OverrideWithEndpointsStrategy(IReadOnlyCollection<ListenOptions> endpoints, string[] originalAddresses)
                : base(endpoints)
            {
                _originalAddresses = originalAddresses;
            }

            public override Task BindAsync(AddressBindContext context)
            {
                var joined = string.Join(", ", _originalAddresses);
                context.Logger.LogWarning(CoreStrings.OverridingWithKestrelOptions, joined, "UseKestrel()");

                return base.BindAsync(context);
            }
        }

        private class EndpointsStrategy : IStrategy
        {
            private readonly IReadOnlyCollection<ListenOptions> _endpoints;

            public EndpointsStrategy(IReadOnlyCollection<ListenOptions> endpoints)
            {
                _endpoints = endpoints;
            }

            public virtual async Task BindAsync(AddressBindContext context)
            {
                foreach (var endpoint in _endpoints)
                {
                    await BindEndpointAsync(endpoint, context).ConfigureAwait(false);

                    context.Addresses.Add(endpoint.GetDisplayName());
                }
            }
        }

        private class AddressesStrategy : IStrategy
        {
            protected readonly IReadOnlyCollection<string> _addresses;

            public AddressesStrategy(IReadOnlyCollection<string> addresses)
            {
                _addresses = addresses;
            }

            public virtual async Task BindAsync(AddressBindContext context)
            {
                foreach (var address in _addresses)
                {
                    await BindAddressAsync(address, context).ConfigureAwait(false);
                }
            }
        }
    }
}

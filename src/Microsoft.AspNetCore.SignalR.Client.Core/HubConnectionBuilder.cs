// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.SignalR.Client
{
    public class HubConnectionBuilder : IHubConnectionBuilder
    {
        private bool _hubConnectionBuilt;

        public IServiceCollection Services { get; }

        public HubConnectionBuilder()
        {
            Services = new ServiceCollection();
            Services.AddSingleton<HubConnection>();
            Services.AddLogging();
            this.AddJsonProtocol();
        }

        public HubConnection Build()
        {
            // Build can only be used once
            if (_hubConnectionBuilt)
            {
                throw new InvalidOperationException("HubConnectionBuilder allows creation only of a single instance of HubConnection.");
            }

            _hubConnectionBuilt = true;

            // The service provider is disposed by the HubConnection
            var serviceProvider = Services.BuildServiceProvider();

            var connectionFactory = serviceProvider.GetService<IConnectionFactory>();
            if (connectionFactory == null)
            {
                throw new InvalidOperationException($"Cannot create {nameof(HubConnection)} instance. An {nameof(IConnectionFactory)} was not configured.");
            }

            return serviceProvider.GetService<HubConnection>();
        }

        // Prevents from being displayed in intellisense
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        // Prevents from being displayed in intellisense
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        // Prevents from being displayed in intellisense
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override string ToString()
        {
            return base.ToString();
        }

        // Prevents from being displayed in intellisense
        [EditorBrowsable(EditorBrowsableState.Never)]
        public new Type GetType()
        {
            return base.GetType();
        }
    }
}

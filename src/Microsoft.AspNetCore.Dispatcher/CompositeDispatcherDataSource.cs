// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Dispatcher
{
    public class CompositeDispatcherDataSource : DispatcherDataSource
    {
        private readonly DispatcherDataSource[] _dataSources;

        public CompositeDispatcherDataSource(IEnumerable<DispatcherDataSource> dataSources)
        {
            if (dataSources == null)
            {
                throw new ArgumentNullException(nameof(dataSources));
            }

            _dataSources = dataSources.ToArray();

            var changeTokens = new IChangeToken[_dataSources.Length];
            for (var i = 0; i < _dataSources.Length; i++)
            {
                changeTokens[i] = _dataSources[i].ChangeToken;
            }

            ChangeToken = new CompositeChangeToken(changeTokens);
        }

        public override IChangeToken ChangeToken { get; }

        protected override IReadOnlyList<Address> GetAddresses()
        {
            var addresses = new List<Address>();
            for (var i = 0; i < _dataSources.Length; i++)
            {
                addresses.AddRange(((IAddressCollectionProvider)_dataSources[i]).Addresses);
            }

            return addresses;
        }

        protected override IReadOnlyList<Endpoint> GetEndpoints()
        {
            var endpoints = new List<Endpoint>();
            for (var i = 0; i < _dataSources.Length; i++)
            {
                endpoints.AddRange(((IEndpointCollectionProvider)_dataSources[i]).Endpoints);
            }

            return endpoints;
        }
    }
}

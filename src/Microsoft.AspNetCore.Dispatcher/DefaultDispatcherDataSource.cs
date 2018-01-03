// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Dispatcher
{
    public class DefaultDispatcherDataSource : DispatcherDataSource
    {
        private readonly List<Address> _addresses;
        private readonly List<Endpoint> _endpoints; 

        public DefaultDispatcherDataSource()
        {
            _addresses = new List<Address>();
            _endpoints = new List<Endpoint>();
        }

        public override IChangeToken ChangeToken { get; } = NullChangeToken.Singleton;

        public IList<Address> Addresses => _addresses;

        public IList<Endpoint> Endpoints => _endpoints;

        protected override IReadOnlyList<Address> GetAddresses() => _addresses;

        protected override IReadOnlyList<Endpoint> GetEndpoints() => _endpoints;
    }
}

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Dispatcher
{
    public class DefaultAddressTable : AddressTable
    {
        private readonly DispatcherOptions _options;
        private readonly List<Address>[] _groups;

        public DefaultAddressTable(IOptions<DispatcherOptions> options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _options = options.Value;

            _groups = new List<Address>[options.Value.Matchers.Count];
            for (var i = 0; i < options.Value.Matchers.Count; i++)
            {
                _groups[i] = new List<Address>(options.Value.Matchers[i].AddressProvider?.Addresses ?? Array.Empty<Address>());
            }
        }

        public override IReadOnlyList<IReadOnlyList<Address>> AddressGroups => _groups;
    }
}

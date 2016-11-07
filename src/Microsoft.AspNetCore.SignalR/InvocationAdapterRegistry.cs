// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.SignalR
{
    public class InvocationAdapterRegistry
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly SignalROptions _options;

        public InvocationAdapterRegistry(IOptions<SignalROptions> options, IServiceProvider serviceProvider)
        {
            _options = options.Value;
            _serviceProvider = serviceProvider;
        }

        public IInvocationAdapter GetInvocationAdapter(string format)
        {
            Type type;
            if (_options._invocationMappings.TryGetValue(format, out type))
            {
                return _serviceProvider.GetRequiredService(type) as IInvocationAdapter;
            }

            return null;
        }
    }
}
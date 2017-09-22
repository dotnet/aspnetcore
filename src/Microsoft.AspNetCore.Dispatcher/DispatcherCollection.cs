// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.ObjectModel;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Dispatcher
{
    public class DispatcherCollection : Collection<DispatcherEntry>
    {
        public void Add(DispatcherBase dispatcher)
        {
            if (dispatcher == null)
            {
                throw new ArgumentNullException(nameof(dispatcher));
            }

            Add(new DispatcherEntry()
            {
                Dispatcher = dispatcher.InvokeAsync,
                AddressProvider  = dispatcher,
                EndpointProvider = dispatcher,
            });
        }

        public void Add(RequestDelegate dispatcher)
        {
            if (dispatcher == null)
            {
                throw new ArgumentNullException(nameof(dispatcher));
            }

            Add(new DispatcherEntry()
            {
                Dispatcher = dispatcher,
            });
        }
    }
}

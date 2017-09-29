// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Dispatcher
{
    public class DispatcherEntry
    {
        public RequestDelegate Dispatcher { get; set; }

        public IAddressCollectionProvider AddressProvider { get; set; }

        public IEndpointCollectionProvider EndpointProvider { get; set; }
    }
}

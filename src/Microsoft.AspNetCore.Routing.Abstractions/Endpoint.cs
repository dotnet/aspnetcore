// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.AspNetCore.Routing
{
    [DebuggerDisplay("{DisplayName,nq}")]
    public abstract class Endpoint
    {
        protected Endpoint(
            EndpointMetadataCollection metadata,
            string displayName,
            Address address)
        {
            // All are allowed to be null
            Metadata = metadata ?? EndpointMetadataCollection.Empty;
            DisplayName = displayName;
            Address = address;
        }

        public string DisplayName { get; }

        public EndpointMetadataCollection Metadata { get; }

        public Address Address { get; }
    }
}

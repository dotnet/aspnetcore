// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;

namespace Microsoft.AspNetCore.SignalR
{
    /// <summary>
    /// Builds conventions that will be used for customization of ComponentHub <see cref="EndpointBuilder"/> instances.
    /// </summary>
    public class ComponentEndpointConventionBuilder : HubEndpointConventionBuilder
    {
        internal ComponentEndpointConventionBuilder(IEndpointConventionBuilder endpointConventionBuilder) : base(endpointConventionBuilder)
        {
        }
    }
}

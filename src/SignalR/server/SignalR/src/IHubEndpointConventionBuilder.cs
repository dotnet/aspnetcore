// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Abstraction that builds conventions that will be used for customization of Hub <see cref="EndpointBuilder"/> instances.
    /// </summary>
    public interface IHubEndpointConventionBuilder : IEndpointConventionBuilder
    {
    }
}

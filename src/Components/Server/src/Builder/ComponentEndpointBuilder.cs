// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// <see cref="IEndpointConventionBuilder"/> implementation to add specific component extensions.
    /// </summary>
    public sealed class ComponentEndpointBuilder : IEndpointConventionBuilder
    {
        private readonly IEndpointConventionBuilder _builder;

        /// <summary>
        /// Initializes a new instance of <see cref="ComponentEndpointBuilder"/>.
        /// </summary>
        /// <param name="builder"></param>
        public ComponentEndpointBuilder(IEndpointConventionBuilder builder)
        {
            _builder = builder;
        }

        /// <inheritdoc />
        public void Add(Action<EndpointBuilder> convention)
        {
            _builder.Add(convention);
        }
    }
}

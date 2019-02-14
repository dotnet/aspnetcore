// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// <see cref="IEndpointConventionBuilder"/> to add specific component extensions.
    /// </summary>
    public class RazorComponentsEndpointBuilder : IEndpointConventionBuilder
    {
        private readonly IEndpointConventionBuilder _builder;

        internal RazorComponentsEndpointBuilder(IEndpointConventionBuilder builder)
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

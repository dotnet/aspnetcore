// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Builds conventions that will be used for customization of <see cref="EndpointBuilder"/> instances.
    /// </summary>
    /// <remarks>
    /// This interface is used at application startup to customize endpoints for the application.
    /// </remarks>
    public interface IEndpointConventionBuilder
    {
        /// <summary>
        /// Adds the convention to the builder. Conventions will used for customization of <see cref="EndpointBuilder"/> instances.
        /// </summary>
        /// <param name="convention">The convention to add to the builder.</param>
        void Add(Action<EndpointBuilder> convention);
    }
}

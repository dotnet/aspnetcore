// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Routing.Matching;

namespace Microsoft.AspNetCore.Routing
{
    /// <summary>
    /// Defines a policy that applies behaviors to the URL matcher. Implementations
    /// of <see cref="MatcherPolicy"/> and related interfaces must be registered
    /// in the dependency injection container as singleton services of type
    /// <see cref="MatcherPolicy"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="MatcherPolicy"/> implementations can implement the following
    /// interfaces <see cref="IEndpointComparerPolicy"/>, <see cref="IEndpointSelectorPolicy"/>,
    /// and <see cref="INodeBuilderPolicy"/>.
    /// </remarks>
    public abstract class MatcherPolicy
    {
        /// <summary>
        /// Gets a value that determines the order the <see cref="MatcherPolicy"/> should
        /// be applied. Policies are applied in ascending numeric value of the <see cref="Order"/>
        /// property.
        /// </summary>
        public abstract int Order { get; }
    }
}

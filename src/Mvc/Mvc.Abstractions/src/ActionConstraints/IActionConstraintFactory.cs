// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Mvc.ActionConstraints
{
    /// <summary>
    /// A factory for <see cref="IActionConstraint"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="IActionConstraintFactory"/> will be invoked during action selection
    /// to create constraint instances for an action.
    ///
    /// Place an attribute implementing this interface on a controller or action to insert an action
    /// constraint created by a factory.
    /// </remarks>
    public interface IActionConstraintFactory : IActionConstraintMetadata
    {
        /// <summary>
        /// Gets a value that indicates if the result of <see cref="CreateInstance(IServiceProvider)"/>
        /// can be reused across requests.
        /// </summary>
        bool IsReusable { get; }

        /// <summary>
        /// Creates a new <see cref="IActionConstraint"/>.
        /// </summary>
        /// <param name="services">The per-request services.</param>
        /// <returns>An <see cref="IActionConstraint"/>.</returns>
        IActionConstraint CreateInstance(IServiceProvider services);
    }
}
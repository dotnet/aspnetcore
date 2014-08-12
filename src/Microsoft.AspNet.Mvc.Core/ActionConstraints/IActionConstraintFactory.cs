// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// A factory for <see cref="IActionConstraint"/>. 
    /// </summary>
    /// <remarks>
    /// <see cref="IActionConstraintFactory"/> will be invoked by <see cref="DefaultActionConstraintProvider"/>
    /// to create constraint instances for an action.
    /// 
    /// Place an attribute implementing this interface on a controller or action to insert an action
    /// constraint created by a factory.
    /// </remarks>
    public interface IActionConstraintFactory : IActionConstraintMetadata
    {
        /// <summary>
        /// Creates a new <see cref="IActionConstraint"/>.
        /// </summary>
        /// <param name="services">The per-request services.</param>
        /// <returns>An <see cref="IActionConstraint"/>.</returns>
        IActionConstraint CreateInstance(IServiceProvider services);
    }
}
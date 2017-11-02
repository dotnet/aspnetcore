// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Dispatcher
{
    /// <summary>
    /// Defines an abstraction for resolving constraints as instances of <see cref="IDispatcherValueConstraint"/>.
    /// </summary>
    public interface IConstraintFactory
    {
        /// <summary>
        /// Resolves the constraint.
        /// </summary>
        /// <param name="constraint">The constraint to resolve.</param>
        /// <returns>The <see cref="IDispatcherValueConstraint"/> the constraint was resolved to.</returns>
        IDispatcherValueConstraint ResolveConstraint(string constraint);
    }
}
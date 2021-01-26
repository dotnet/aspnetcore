// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Routing
{
    /// <summary>
    /// Defines an abstraction for resolving inline constraints as instances of <see cref="IRouteConstraint"/>.
    /// </summary>
    public interface IInlineConstraintResolver
    {
        /// <summary>
        /// Resolves the inline constraint.
        /// </summary>
        /// <param name="inlineConstraint">The inline constraint to resolve.</param>
        /// <returns>The <see cref="IRouteConstraint"/> the inline constraint was resolved to.</returns>
        IRouteConstraint? ResolveConstraint(string inlineConstraint);
    }
}

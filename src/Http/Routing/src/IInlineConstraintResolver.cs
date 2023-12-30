// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Routing;

#if !COMPONENTS
/// <summary>
/// Defines an abstraction for resolving inline constraints as instances of <see cref="IRouteConstraint"/>.
/// </summary>
public interface IInlineConstraintResolver
#else
internal interface IInlineConstraintResolver
#endif
{
    /// <summary>
    /// Resolves the inline constraint.
    /// </summary>
    /// <param name="inlineConstraint">The inline constraint to resolve.</param>
    /// <returns>The <see cref="IRouteConstraint"/> the inline constraint was resolved to.</returns>
    IRouteConstraint? ResolveConstraint(string inlineConstraint);
}

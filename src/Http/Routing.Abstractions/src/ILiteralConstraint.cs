// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Routing
{
    /// <summary>
    /// Defines the contract that a class must implement in order to check if a literal value is valid for a given constraint.
    /// <remarks>
    /// When a parameter implements this interface, the router is able to optimize away some paths from the route table that don't match this constraint.
    /// </remarks>
    /// </summary>
    public interface ILiteralConstraint : IParameterPolicy
    {
        /// <summary>
        /// Determines whether the given <paramref name="literal"/> can match the constraint.
        /// </summary>
        /// <param name="literal">The literal to test the constraint against.</param>
        /// <returns><c>true</c> if the literal contains a valid value; otherwise, <c>false</c>.</returns>
        bool MatchLiteral(string literal);
    }
}

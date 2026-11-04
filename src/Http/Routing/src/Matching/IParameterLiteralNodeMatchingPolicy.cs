// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Routing.Matching;

/// <summary>
/// Defines the contract that a class must implement in order to check if a literal value is valid for a given constraint.
/// <remarks>
/// When a parameter implements this interface, the router is able to optimize away some paths from the route table that don't match this constraint.
/// </remarks>
/// </summary>
public interface IParameterLiteralNodeMatchingPolicy : IParameterPolicy
{
    /// <summary>
    /// Determines whether the given <paramref name="literal"/> can match the constraint.
    /// </summary>
    /// <param name="parameterName">The parameter name we are currently evaluating.</param>
    /// <param name="literal">The literal to test the constraint against.</param>
    /// <returns><c>true</c> if the literal contains a valid value; otherwise, <c>false</c>.</returns>
    bool MatchesLiteral(string parameterName, string literal);
}

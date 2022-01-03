// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing.Matching;

/// <summary>
/// A <see cref="MatcherPolicy"/> interface that can implemented to filter endpoints
/// in a <see cref="CandidateSet"/>. Implementations of <see cref="IEndpointSelectorPolicy"/> must
/// inherit from <see cref="MatcherPolicy"/> and should be registered in
/// the dependency injection container as singleton services of type <see cref="MatcherPolicy"/>.
/// </summary>
public interface IEndpointSelectorPolicy
{
    /// <summary>
    /// Returns a value that indicates whether the <see cref="IEndpointSelectorPolicy"/> applies
    /// to any endpoint in <paramref name="endpoints"/>.
    /// </summary>
    /// <param name="endpoints">The set of candidate <see cref="Endpoint"/> values.</param>
    /// <returns>
    /// <c>true</c> if the policy applies to any endpoint in <paramref name="endpoints"/>, otherwise <c>false</c>.
    /// </returns>
    bool AppliesToEndpoints(IReadOnlyList<Endpoint> endpoints);

    /// <summary>
    /// Applies the policy to the <see cref="CandidateSet"/>.
    /// </summary>
    /// <param name="httpContext">
    /// The <see cref="HttpContext"/> associated with the current request.
    /// </param>
    /// <param name="candidates">The <see cref="CandidateSet"/>.</param>
    /// <remarks>
    /// <para>
    /// Implementations of <see cref="IEndpointSelectorPolicy"/> should implement this method
    /// and filter the set of candidates in the <paramref name="candidates"/> by setting
    /// <see cref="CandidateSet.SetValidity(int, bool)"/> to <c>false</c> where desired.
    /// </para>
    /// <para>
    /// To signal an error condition, the <see cref="IEndpointSelectorPolicy"/> should assign the endpoint by
    /// calling <see cref="EndpointHttpContextExtensions.SetEndpoint(HttpContext, Endpoint)"/>
    /// and setting <see cref="HttpRequest.RouteValues"/> to an
    /// <see cref="Endpoint"/> value that will produce the desired error when executed.
    /// </para>
    /// </remarks>
    Task ApplyAsync(HttpContext httpContext, CandidateSet candidates);
}

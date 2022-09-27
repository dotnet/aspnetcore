// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing.Matching;

/// <summary>
/// A <see cref="MatcherPolicy"/> interface that can be implemented to sort
/// endpoints. Implementations of <see cref="IEndpointComparerPolicy"/> must
/// inherit from <see cref="MatcherPolicy"/> and should be registered in
/// the dependency injection container as singleton services of type <see cref="MatcherPolicy"/>.
/// </summary>
/// <remarks>
/// <para>
/// Candidates in a <see cref="CandidateSet"/> are sorted based on their priority. Defining
/// a <see cref="IEndpointComparerPolicy"/> adds an additional criterion to the sorting
/// operation used to order candidates.
/// </para>
/// <para>
/// As an example, the implementation of <see cref="HttpMethodMatcherPolicy"/> implements
/// <see cref="IEndpointComparerPolicy"/> to ensure that endpoints matching specific HTTP
/// methods are sorted with a higher priority than endpoints without a specific HTTP method
/// requirement.
/// </para>
/// </remarks>
public interface IEndpointComparerPolicy
{
    /// <summary>
    /// Gets an <see cref="IComparer{Endpoint}"/> that will be used to sort the endpoints.
    /// </summary>
    IComparer<Endpoint> Comparer { get; }
}

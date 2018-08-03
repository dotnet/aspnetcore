// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing.Matching
{
    /// <summary>
    /// A <see cref="MatcherPolicy"/> interface that can implemented to filter endpoints
    /// in a <see cref="CandidateSet"/>. Implementations of <see cref="IEndpointSelectorPolicy"/> must
    /// inherit from <see cref="MatcherPolicy"/> and should be registered in
    /// the dependency injection container as singleton services of type <see cref="MatcherPolicy"/>.
    /// </summary>
    public interface IEndpointSelectorPolicy
    {
        /// <summary>
        /// Applies the policy to the <see cref="CandidateSet"/>.
        /// </summary>
        /// <param name="httpContext">
        /// The <see cref="HttpContext"/> associated with the current request.
        /// </param>
        /// <param name="candidates">The <see cref="CandidateSet"/>.</param>
        /// <remarks>
        /// Implementations of <see cref="IEndpointSelectorPolicy"/> should implement this method
        /// and filter the set of candidates in the <paramref name="candidates"/> by setting
        /// <see cref="CandidateState.IsValidCandidate"/> to <c>false</c> where desired.
        /// </remarks>
        void Apply(HttpContext httpContext, CandidateSet candidates);
    }
}

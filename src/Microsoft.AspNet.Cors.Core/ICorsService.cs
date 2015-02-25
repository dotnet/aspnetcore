// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Http;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Cors.Core
{
    /// <summary>
    /// A type which can evaluate a policy for a particular <see cref="HttpContext"/>.
    /// </summary>
    public interface ICorsService
    {
        /// <summary>
        /// Evaluates the given <paramref name="policy"/> using the passed in <paramref name="context"/>.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/> associated with the call.</param>
        /// <param name="policy">The <see cref="CorsPolicy"/> which needs to be evaluated.</param>
        /// <returns>A <see cref="CorsResult"/> which contains the result of policy evaluation and can be
        /// used by the caller to set apporpriate response headers.</returns>
        CorsResult EvaluatePolicy([NotNull] HttpContext context, [NotNull] CorsPolicy policy);


        /// <summary>
        /// Adds CORS-specific response headers to the given <paramref name="response"/>.
        /// </summary>
        /// <param name="result">The <see cref="CorsResult"/> used to read the allowed values.</param>
        /// <param name="response">The <see cref="HttpResponse"/> associated with the current call.</param>
        void ApplyResult([NotNull] CorsResult result, [NotNull] HttpResponse response);

    }
}
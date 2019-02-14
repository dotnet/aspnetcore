// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.Infrastructure
{
    /// <summary>
    /// A factory for producing client errors. This contract is used by controllers annotated
    /// with <see cref="ApiControllerAttribute"/> to transform <see cref="IClientErrorActionResult"/>.
    /// </summary>
    public interface IClientErrorFactory
    {
        /// <summary>
        /// Transforms <paramref name="clientError"/> for the specified <paramref name="actionContext"/>.
        /// </summary>
        /// <param name="actionContext">The <see cref="ActionContext"/>.</param>
        /// <param name="clientError">The <see cref="IClientErrorActionResult"/>.</param>
        /// <returns>THe <see cref="IActionResult"/> that would be returned to the client.</returns>
        IActionResult GetClientError(ActionContext actionContext, IClientErrorActionResult clientError);
    }
}

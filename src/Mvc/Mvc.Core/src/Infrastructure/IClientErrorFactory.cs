// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

namespace Microsoft.AspNetCore.Mvc.Infrastructure;

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
    /// <returns>The <see cref="IActionResult"/> that would be returned to the client.</returns>
    IActionResult? GetClientError(ActionContext actionContext, IClientErrorActionResult clientError);
}

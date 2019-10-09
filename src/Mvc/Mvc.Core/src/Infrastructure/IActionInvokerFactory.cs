// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.Abstractions;

namespace Microsoft.AspNetCore.Mvc.Infrastructure
{
    /// <summary>
    /// Defines an interface for creating an <see cref="IActionInvoker"/> for the current request. 
    /// </summary>
    /// <remarks>
    /// The default <see cref="IActionInvokerFactory"/> implementation creates an <see cref="IActionInvoker"/> by
    /// calling into each <see cref="IActionInvokerProvider"/>. See <see cref="IActionInvokerProvider"/> for more
    /// details.
    /// </remarks>
    public interface IActionInvokerFactory
    {
        /// <summary>
        /// Creates an <see cref="IActionInvoker"/> for the current request associated with
        /// <paramref name="actionContext"/>. 
        /// </summary>
        /// <param name="actionContext">
        /// The <see cref="ActionContext"/> associated with the current request.
        /// </param>
        /// <returns>An <see cref="IActionInvoker"/> or <c>null</c>.</returns>
        IActionInvoker CreateInvoker(ActionContext actionContext);
    }
}

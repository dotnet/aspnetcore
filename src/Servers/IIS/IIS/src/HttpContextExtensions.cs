// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Server.IIS
{
    /// <summary>
    /// Extensions to <see cref="HttpContext"/> that enable access to IIS features.
    /// </summary>
    public static class HttpContextExtensions
    {
        /// <summary>
        /// Gets the value of a server variable for the current request.
        /// </summary>
        /// <param name="context">The http context for the request.</param>
        /// <param name="variableName">The name of the variable.</param>
        /// <returns>
        /// <c>null</c> if the server does not support the <see cref="IServerVariablesFeature"/> feature.
        /// May return null or empty if the variable does not exist or is not set.
        /// </returns>
        [Obsolete("This is obsolete and will be removed in a future version. Use " + nameof(HttpContextServerVariableExtensions.GetServerVariable) + " instead.")]
        public static string GetIISServerVariable(this HttpContext context, string variableName) =>
            context.GetServerVariable(variableName);
    }
}

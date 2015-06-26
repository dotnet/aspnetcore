// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.HttpOverrides;

namespace Microsoft.AspNet.Builder
{
    public static class HttpMethodOverrideExtensions
    {
        /// <summary>
        /// Allows incoming POST request to override method type with type specified in header.
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseHttpMethodOverride(this IApplicationBuilder builder)
        {
            return builder.Use(next => new HttpMethodOverrideMiddleware(next).Invoke);
        }

        /// <summary>
        /// Allows incoming POST request to override method type with type specified in form.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="formFieldInput">Denotes the element that contains the name of the resulting method type.</param>
        /// <returns></returns>
        public static IApplicationBuilder UseHttpMethodOverride(this IApplicationBuilder builder, string formFieldInput)
        {
            return builder.Use(next => new HttpMethodOverrideMiddleware(next, formFieldInput).Invoke);
        }
    }
}

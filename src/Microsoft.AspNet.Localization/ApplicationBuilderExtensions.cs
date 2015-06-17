// Copyright (c) .NET Foundation. All rights reserved. 
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using Microsoft.AspNet.Localization;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Builder
{
    /// <summary>
    /// Extension methods for adding the <see cref="RequestLocalizationMiddleware"/> to an application.
    /// </summary>
    public static class ApplicationBuilderExtensions
    {
        /// <summary>
        /// Adds the <see cref="RequestLocalizationMiddleware"/> to automatically set culture information for
        /// requests based on information provided by the client using the default options.
        /// </summary>
        /// <param name="builder">The <see cref="IApplicationBuilder"/>.</param>
        /// <returns>The <see cref="IApplicationBuilder"/>.</returns>
        public static IApplicationBuilder UseRequestLocalization([NotNull] this IApplicationBuilder builder)
        {
            var options = new RequestLocalizationOptions();

            return UseRequestLocalization(builder, options);
        }

        /// <summary>
        /// Adds the <see cref="RequestLocalizationMiddleware"/> to automatically set culture information for
        /// requests based on information provided by the client.
        /// </summary>
        /// <param name="builder">The <see cref="IApplicationBuilder"/>.</param>
        /// <param name="options">The options to configure the middleware with.</param>
        /// <returns>The <see cref="IApplicationBuilder"/>.</returns>
        public static IApplicationBuilder UseRequestLocalization(
            [NotNull] this IApplicationBuilder builder,
            [NotNull] RequestLocalizationOptions options)
            => builder.UseMiddleware<RequestLocalizationMiddleware>(options);
    }
}
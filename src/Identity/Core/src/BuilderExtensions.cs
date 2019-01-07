// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Identity extensions for <see cref="IApplicationBuilder"/>.
    /// </summary>
    public static class BuilderExtensions
    {
        /// <summary>
        /// <para>
        /// This method is obsolete and will be removed in a future version.
        /// The recommended alternative is <see cref="AuthAppBuilderExtensions.UseAuthentication(IApplicationBuilder)" />
        /// </para>
        /// <para>
        /// Enables ASP.NET identity for the current application.
        /// </para>
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/> instance this method extends.</param>
        /// <returns>The <see cref="IApplicationBuilder"/> instance this method extends.</returns>
        [Obsolete(
            "This method is obsolete and will be removed in a future version. " +
            "The recommended alternative is UseAuthentication(). " +
            "See https://go.microsoft.com/fwlink/?linkid=845470")]
        public static IApplicationBuilder UseIdentity(this IApplicationBuilder app)
            => app.UseAuthentication();
    }
}

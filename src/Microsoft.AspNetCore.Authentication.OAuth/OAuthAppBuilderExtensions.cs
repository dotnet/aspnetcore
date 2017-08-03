// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Authentication.OAuth;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Extension methods to add OAuth 2.0 authentication capabilities to an HTTP application pipeline.
    /// </summary>
    public static class OAuthAppBuilderExtensions
    {
        /// <summary>
        /// UseOAuthAuthentication is obsolete. Configure OAuth authentication with AddAuthentication().AddOAuth in ConfigureServices. See https://go.microsoft.com/fwlink/?linkid=845470 for more details.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/> to add the handler to.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        [Obsolete("UseOAuthAuthentication is obsolete. Configure OAuth authentication with AddAuthentication().AddOAuth in ConfigureServices. See https://go.microsoft.com/fwlink/?linkid=845470 for more details.", error: true)]
        public static IApplicationBuilder UseOAuthAuthentication(this IApplicationBuilder app)
        {
            throw new NotSupportedException("This method is no longer supported, see https://go.microsoft.com/fwlink/?linkid=845470");
        }

        /// <summary>
        /// UseOAuthAuthentication is obsolete. Configure OAuth authentication with AddAuthentication().AddOAuth in ConfigureServices. See https://go.microsoft.com/fwlink/?linkid=845470 for more details.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/> to add the handler to.</param>
        /// <param name="options">A <see cref="OAuthOptions"/> that specifies options for the handler.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        [Obsolete("UseOAuthAuthentication is obsolete. Configure OAuth authentication with AddAuthentication().AddOAuth in ConfigureServices. See https://go.microsoft.com/fwlink/?linkid=845470 for more details.", error: true)]
        public static IApplicationBuilder UseOAuthAuthentication(this IApplicationBuilder app, OAuthOptions options)
        {
            throw new NotSupportedException("This method is no longer supported, see https://go.microsoft.com/fwlink/?linkid=845470");
        }
    }
}
// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Authentication.Twitter;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Extension methods to add Twitter authentication capabilities to an HTTP application pipeline.
    /// </summary>
    public static class TwitterAppBuilderExtensions
    {
        /// <summary>
        /// UseTwitterAuthentication is obsolete. Configure Twitter authentication with AddAuthentication().AddTwitter in ConfigureServices. See https://go.microsoft.com/fwlink/?linkid=845470 for more details.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/> to add the handler to.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        [Obsolete("UseTwitterAuthentication is obsolete. Configure Twitter authentication with AddAuthentication().AddTwitter in ConfigureServices. See https://go.microsoft.com/fwlink/?linkid=845470 for more details.", error: true)]
        public static IApplicationBuilder UseTwitterAuthentication(this IApplicationBuilder app)
        {
            throw new NotSupportedException("This method is no longer supported, see https://go.microsoft.com/fwlink/?linkid=845470");
        }

        /// <summary>
        /// UseTwitterAuthentication is obsolete. Configure Twitter authentication with AddAuthentication().AddTwitter in ConfigureServices. See https://go.microsoft.com/fwlink/?linkid=845470 for more details.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/> to add the handler to.</param>
        /// <param name="options">An action delegate to configure the provided <see cref="TwitterOptions"/>.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        [Obsolete("UseTwitterAuthentication is obsolete. Configure Twitter authentication with AddAuthentication().AddTwitter in ConfigureServices. See https://go.microsoft.com/fwlink/?linkid=845470 for more details.", error: true)]
        public static IApplicationBuilder UseTwitterAuthentication(this IApplicationBuilder app, TwitterOptions options)
        {
            throw new NotSupportedException("This method is no longer supported, see https://go.microsoft.com/fwlink/?linkid=845470");
        }
    }
}
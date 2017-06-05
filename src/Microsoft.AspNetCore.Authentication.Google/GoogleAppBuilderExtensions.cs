// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Extension methods to add Google authentication capabilities to an HTTP application pipeline.
    /// </summary>
    public static class GoogleAppBuilderExtensions
    {
        /// <summary>
        /// Obsolete, see https://go.microsoft.com/fwlink/?linkid=845470
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/> to add the handler to.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IApplicationBuilder UseGoogleAuthentication(this IApplicationBuilder app)
        {
            throw new NotSupportedException("This method is no longer supported, see https://go.microsoft.com/fwlink/?linkid=845470");
        }

        /// <summary>
        /// Obsolete, see https://go.microsoft.com/fwlink/?linkid=845470
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/> to add the handler to.</param>
        /// <param name="options">A <see cref="GoogleOptions"/> that specifies options for the handler.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IApplicationBuilder UseGoogleAuthentication(this IApplicationBuilder app, GoogleOptions options)
        {
            throw new NotSupportedException("This method is no longer supported, see https://go.microsoft.com/fwlink/?linkid=845470");
        }
    }
}

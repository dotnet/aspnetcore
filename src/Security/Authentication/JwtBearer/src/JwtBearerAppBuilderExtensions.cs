// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Extension methods to add OpenIdConnect Bearer authentication capabilities to an HTTP application pipeline.
    /// </summary>
    public static class JwtBearerAppBuilderExtensions
    {
        /// <summary>
        /// UseJwtBearerAuthentication is obsolete. Configure JwtBearer authentication with AddAuthentication().AddJwtBearer in ConfigureServices. See https://go.microsoft.com/fwlink/?linkid=845470 for more details.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/> to add the handler to.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        [Obsolete("UseJwtBearerAuthentication is obsolete. Configure JwtBearer authentication with AddAuthentication().AddJwtBearer in ConfigureServices. See https://go.microsoft.com/fwlink/?linkid=845470 for more details.", error: true)]
        public static IApplicationBuilder UseJwtBearerAuthentication(this IApplicationBuilder app)
        {
            throw new NotSupportedException("This method is no longer supported, see https://go.microsoft.com/fwlink/?linkid=845470");
        }

        /// <summary>
        /// UseJwtBearerAuthentication is obsolete. Configure JwtBearer authentication with AddAuthentication().AddJwtBearer in ConfigureServices. See https://go.microsoft.com/fwlink/?linkid=845470 for more details.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/> to add the handler to.</param>
        /// <param name="options">A  <see cref="JwtBearerOptions"/> that specifies options for the handler.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        [Obsolete("UseJwtBearerAuthentication is obsolete. Configure JwtBearer authentication with AddAuthentication().AddJwtBearer in ConfigureServices. See https://go.microsoft.com/fwlink/?linkid=845470 for more details.", error: true)]
        public static IApplicationBuilder UseJwtBearerAuthentication(this IApplicationBuilder app, JwtBearerOptions options)
        {
            throw new NotSupportedException("This method is no longer supported, see https://go.microsoft.com/fwlink/?linkid=845470");
        }
    }
}
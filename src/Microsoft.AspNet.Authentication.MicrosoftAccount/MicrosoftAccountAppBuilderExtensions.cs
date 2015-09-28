// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Authentication.MicrosoftAccount;

namespace Microsoft.AspNet.Builder
{
    /// <summary>
    /// Extension methods for using <see cref="MicrosoftAccountMiddleware"/>
    /// </summary>
    public static class MicrosoftAccountAuthenticationExtensions
    {
        public static IApplicationBuilder UseMicrosoftAccountAuthentication(this IApplicationBuilder app, MicrosoftAccountOptions options)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            return app.UseMiddleware<MicrosoftAccountMiddleware>(options);
        }

        public static IApplicationBuilder UseMicrosoftAccountAuthentication(this IApplicationBuilder app, Action<MicrosoftAccountOptions> configureOptions)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            var options = new MicrosoftAccountOptions();
            if (configureOptions != null)
            {
                configureOptions(options);
            }
            return app.UseMicrosoftAccountAuthentication(options);
        }
    }
}

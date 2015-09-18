// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Authentication.MicrosoftAccount;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Builder
{
    /// <summary>
    /// Extension methods for using <see cref="MicrosoftAccountMiddleware"/>
    /// </summary>
    public static class MicrosoftAccountAuthenticationExtensions
    {
        public static IApplicationBuilder UseMicrosoftAccountAuthentication([NotNull] this IApplicationBuilder app, [NotNull] MicrosoftAccountOptions options)
        {
            return app.UseMiddleware<MicrosoftAccountMiddleware>(options);
        }

        public static IApplicationBuilder UseMicrosoftAccountAuthentication([NotNull] this IApplicationBuilder app, Action<MicrosoftAccountOptions> configureOptions)
        {
            var options = new MicrosoftAccountOptions();
            if (configureOptions != null)
            {
                configureOptions(options);
            }
            return app.UseMicrosoftAccountAuthentication(options);
        }
    }
}

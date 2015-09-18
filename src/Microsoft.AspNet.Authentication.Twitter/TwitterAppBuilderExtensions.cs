// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Authentication.Twitter;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Builder
{
    /// <summary>
    /// Extension methods for using <see cref="TwitterMiddleware"/>
    /// </summary>
    public static class TwitterAppBuilderExtensions
    {
        public static IApplicationBuilder UseTwitterAuthentication([NotNull] this IApplicationBuilder app, Action<TwitterOptions> configureOptions = null)
        {
            var options = new TwitterOptions();
            if (configureOptions != null)
            {
                configureOptions(options);
            }
            return app.UseTwitterAuthentication(options);
        }

        public static IApplicationBuilder UseTwitterAuthentication([NotNull] this IApplicationBuilder app, [NotNull] TwitterOptions options)
        {
            return app.UseMiddleware<TwitterMiddleware>(options);
        }

    }
}

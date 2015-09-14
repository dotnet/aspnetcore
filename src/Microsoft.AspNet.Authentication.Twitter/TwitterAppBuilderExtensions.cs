// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Authentication.Twitter;
using Microsoft.Framework.Internal;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.AspNet.Builder
{
    /// <summary>
    /// Extension methods for using <see cref="TwitterMiddleware"/>
    /// </summary>
    public static class TwitterAppBuilderExtensions
    {
        public static IApplicationBuilder UseTwitterAuthentication([NotNull] this IApplicationBuilder app, Action<TwitterOptions> configureOptions = null)
        {
            return app.UseMiddleware<TwitterMiddleware>(
                 new ConfigureOptions<TwitterOptions>(configureOptions ?? (o => { })));
        }
    }
}

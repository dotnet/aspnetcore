// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Authentication.MicrosoftAccount;
using Microsoft.Framework.Internal;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.AspNet.Builder
{
    /// <summary>
    /// Extension methods for using <see cref="MicrosoftAccountMiddleware"/>
    /// </summary>
    public static class MicrosoftAccountAuthenticationExtensions
    {
        public static IApplicationBuilder UseMicrosoftAccountAuthentication([NotNull] this IApplicationBuilder app, Action<MicrosoftAccountOptions> configureOptions = null)
        {
            return app.UseMiddleware<MicrosoftAccountMiddleware>(
                 new ConfigureOptions<MicrosoftAccountOptions>(configureOptions ?? (o => { })));
        }
    }
}

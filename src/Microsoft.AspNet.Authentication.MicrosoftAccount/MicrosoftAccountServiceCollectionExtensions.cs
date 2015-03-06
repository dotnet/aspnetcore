// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Authentication.MicrosoftAccount;

namespace Microsoft.Framework.DependencyInjection
{
    /// <summary>
    /// Extension methods for using <see cref="MicrosoftAccountAuthenticationMiddleware"/>
    /// </summary>
    public static class MicrosoftAccountServiceCollectionExtensions
    {
        public static IServiceCollection ConfigureMicrosoftAccountAuthentication([NotNull] this IServiceCollection services, [NotNull] Action<MicrosoftAccountAuthenticationOptions> configure)
        {
            return services.Configure(configure);
        }
    }
}

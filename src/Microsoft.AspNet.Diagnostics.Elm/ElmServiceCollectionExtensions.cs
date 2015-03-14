// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Diagnostics.Elm;
using Microsoft.Framework.Internal;

namespace Microsoft.Framework.DependencyInjection
{
    public static class ElmServiceCollectionExtensions
    {
        /// <summary>
        /// Registers an <see cref="ElmStore"/> and configures <see cref="ElmOptions"/>.
        /// </summary>
        public static IServiceCollection AddElm([NotNull] this IServiceCollection services)
        {
            return services.AddElm(configureOptions: null);
        }

        /// <summary>
        /// Registers an <see cref="ElmStore"/> and configures <see cref="ElmOptions"/>.
        /// </summary>
        public static IServiceCollection AddElm([NotNull] this IServiceCollection services, Action<ElmOptions> configureOptions)
        {
            services.AddSingleton<ElmStore>(); // registering the service so it can be injected into constructors

            if (configureOptions != null)
            {
                services.Configure(configureOptions);
            }

            return services;
        }
    }
}
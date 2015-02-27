// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.Internal;
using Microsoft.Framework.WebEncoders;

namespace Microsoft.Framework.DependencyInjection
{
    public static class EncoderServiceCollectionExtensions
    {
        public static IServiceCollection AddWebEncoders([NotNull] this IServiceCollection services)
        {
            return AddWebEncoders(services, configureOptions: null);
        }

        public static IServiceCollection AddWebEncoders([NotNull] this IServiceCollection services, Action<WebEncoderOptions> configureOptions)
        {
            services.AddOptions();
            services.TryAdd(EncoderServices.GetDefaultServices());
            if (configureOptions != null)
            {
                services.Configure(configureOptions);
            }
            return services;
        }
    }
}

// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.Internal;
using Microsoft.Framework.WebEncoders;

namespace Microsoft.Framework.DependencyInjection
{
    public static class EncoderServiceCollectionExtensions
    {
        public static IServiceCollection AddEncoders([NotNull] this IServiceCollection services)
        {
            return AddEncoders(services, configuration: null);
        }

        public static IServiceCollection AddEncoders([NotNull] this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions(configuration);
            services.TryAdd(EncoderServices.GetDefaultServices(configuration));
            return services;
        }
    }
}

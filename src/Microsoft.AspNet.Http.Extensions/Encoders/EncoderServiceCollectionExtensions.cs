// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Http;
using Microsoft.Framework.WebEncoders;
using Microsoft.Framework.ConfigurationModel;

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

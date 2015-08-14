// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;
using Microsoft.Framework.DependencyInjection.Extensions;
using Microsoft.Framework.Internal;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.Framework.DependencyInjection
{
    public static class MvcDataAnnotationsMvcCoreBuilderExtensions
    {
        public static IMvcCoreBuilder AddDataAnnotations([NotNull] this IMvcCoreBuilder builder)
        {
            AddDataAnnotationsServices(builder.Services);
            return builder;
        }

        // Internal for testing.
        internal static void AddDataAnnotationsServices(IServiceCollection services)
        {
            services.TryAddEnumerable(
                ServiceDescriptor.Transient<IConfigureOptions<MvcOptions>, MvcDataAnnotationsMvcOptionsSetup>());
        }
    }
}

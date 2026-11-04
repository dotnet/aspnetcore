// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.DataAnnotations;

internal static class DataAnnotationsLocalizationServices
{
    public static void AddDataAnnotationsLocalizationServices(
        IServiceCollection services,
        Action<MvcDataAnnotationsLocalizationOptions>? setupAction)
    {
        services.AddLocalization();

        if (setupAction != null)
        {
            services.Configure(setupAction);
        }
        else
        {
            services.TryAddEnumerable(
                ServiceDescriptor.Transient
                <IConfigureOptions<MvcDataAnnotationsLocalizationOptions>,
                MvcDataAnnotationsLocalizationOptionsSetup>());
        }
    }
}

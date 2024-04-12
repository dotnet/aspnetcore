// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Forms;

namespace Microsoft.Extensions.DependencyInjection;

internal static class DefaultAntiforgeryJsonOptionsServiceCollectionExtensions
{
    public static IServiceCollection ConfigureDefaultAntiforgeryJsonOptions(this IServiceCollection services)
    {
        services.ConfigureComponentsJsonOptions(static options =>
        {
            options.SerializerOptions.TypeInfoResolverChain.Insert(0, DefaultAntiforgeryStateProviderJsonSerializerContext.Default);
        });

        return services;
    }
}

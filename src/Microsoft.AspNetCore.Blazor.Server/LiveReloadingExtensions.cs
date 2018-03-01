// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;

namespace Microsoft.AspNetCore.Blazor.Server
{
    internal static class LiveReloadingExtensions
    {
        public static void UseBlazorLiveReloading(
            this IApplicationBuilder applicationBuilder,
            BlazorConfig config)
        {
            if (!string.IsNullOrEmpty(config.ReloadUri))
            {
                var context = new LiveReloadingContext();
                context.Attach(applicationBuilder, config);
            }
        }
    }
}

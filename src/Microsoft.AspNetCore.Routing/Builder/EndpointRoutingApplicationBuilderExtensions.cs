// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Builder
{
    // TODO: Remove once MVC is updated
    public static class EndpointRoutingApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseGlobalRouting(this IApplicationBuilder builder)
        {
            return Internal.EndpointRoutingApplicationBuilderExtensions.UseEndpointRouting(builder);
        }

        public static IApplicationBuilder UseEndpoint(this IApplicationBuilder builder)
        {
            return Internal.EndpointRoutingApplicationBuilderExtensions.UseEndpoint(builder);
        }
    }
}

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;

namespace Microsoft.AspNetCore.Diagnostics.Identity.Service
{
    public static class IdentityApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseDevelopmentCertificateErrorPage(
            this IApplicationBuilder builder,
            IConfiguration configuration)
        {
            builder.UseMiddleware<DeveloperCertificateMiddleware>(configuration);
            return builder;
        }
    }
}

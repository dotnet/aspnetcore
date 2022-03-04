// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Authentication;

namespace Identity.DefaultUI.WebSite
{
    public static class ContosoAuthenticationBuilderExtensions
    {
        public static AuthenticationBuilder AddContosoAuthentication(
            this AuthenticationBuilder builder,
            Action<ContosoAuthenticationOptions> configure) =>
                builder.AddScheme<ContosoAuthenticationOptions, ContosoAuthenticationHandler>(
                    ContosoAuthenticationConstants.Scheme,
                    ContosoAuthenticationConstants.DisplayName,
                    configure);
    }
}

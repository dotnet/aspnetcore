// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class MicrosoftAccountExtensions
    {
        public static AuthenticationBuilder AddMicrosoftAccount(this AuthenticationBuilder builder)
            => builder.AddMicrosoftAccount(MicrosoftAccountDefaults.AuthenticationScheme, _ => { });

        public static AuthenticationBuilder AddMicrosoftAccount(this AuthenticationBuilder builder, Action<MicrosoftAccountOptions> configureOptions)
            => builder.AddMicrosoftAccount(MicrosoftAccountDefaults.AuthenticationScheme, configureOptions);

        public static AuthenticationBuilder AddMicrosoftAccount(this AuthenticationBuilder builder, string authenticationScheme, Action<MicrosoftAccountOptions> configureOptions)
            => builder.AddMicrosoftAccount(authenticationScheme, MicrosoftAccountDefaults.DisplayName, configureOptions);

        public static AuthenticationBuilder AddMicrosoftAccount(this AuthenticationBuilder builder, string authenticationScheme, string displayName, Action<MicrosoftAccountOptions> configureOptions)
            => builder.AddOAuth<MicrosoftAccountOptions, MicrosoftAccountHandler>(authenticationScheme, displayName, configureOptions);
    }
}
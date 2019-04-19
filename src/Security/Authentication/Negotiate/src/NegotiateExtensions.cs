// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class NegotiateExtensions
    {
        public static AuthenticationBuilder AddNegotiate(this AuthenticationBuilder builder)
            => builder.AddNegotiate(NegotiateDefaults.AuthenticationScheme, _ => { });

        public static AuthenticationBuilder AddNegotiate(this AuthenticationBuilder builder, Action<NegotiateOptions> configureOptions)
            => builder.AddNegotiate(NegotiateDefaults.AuthenticationScheme, configureOptions);

        public static AuthenticationBuilder AddNegotiate(this AuthenticationBuilder builder, string authenticationScheme, Action<NegotiateOptions> configureOptions)
            => builder.AddNegotiate(authenticationScheme, displayName: null, configureOptions: configureOptions);

        public static AuthenticationBuilder AddNegotiate(this AuthenticationBuilder builder, string authenticationScheme, string displayName, Action<NegotiateOptions> configureOptions)
        {
            return builder.AddScheme<NegotiateOptions, NegotiateHandler>(authenticationScheme, displayName, configureOptions);
        }
    }
}

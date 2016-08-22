// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Microsoft.AspNetCore.Authentication.Tests.OpenIdConnect
{
    internal class TestDefaultValues
    {
        public static readonly string DefaultAuthority = @"https://login.microsoftonline.com/common";

        public static readonly string TestHost = @"https://example.com";

        public static OpenIdConnectOptions CreateOpenIdConnectOptions() =>
            new OpenIdConnectOptions
            {
                Authority = TestDefaultValues.DefaultAuthority,
                ClientId = Guid.NewGuid().ToString(),
                Configuration = TestDefaultValues.CreateDefaultOpenIdConnectConfiguration()
            };

        public static OpenIdConnectOptions CreateOpenIdConnectOptions(Action<OpenIdConnectOptions> update)
        {
            var options = CreateOpenIdConnectOptions();

            if (update != null)
            {
                update(options);
            }

            return options;
        }

        public static OpenIdConnectConfiguration CreateDefaultOpenIdConnectConfiguration() =>
            new OpenIdConnectConfiguration()
            {
                AuthorizationEndpoint = DefaultAuthority + "/oauth2/authorize",
                EndSessionEndpoint = DefaultAuthority + "/oauth2/endsessionendpoint",
                TokenEndpoint = DefaultAuthority + "/oauth2/token"
            };

        public static IConfigurationManager<OpenIdConnectConfiguration> CreateDefaultOpenIdConnectConfigurationManager() =>
            new StaticConfigurationManager<OpenIdConnectConfiguration>(CreateDefaultOpenIdConnectConfiguration());
    }
}
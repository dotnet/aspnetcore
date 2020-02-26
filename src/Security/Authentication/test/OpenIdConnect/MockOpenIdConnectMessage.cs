// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Microsoft.AspNetCore.Authentication.Test.OpenIdConnect
{
    internal class MockOpenIdConnectMessage : OpenIdConnectMessage
    {
        public string TestAuthorizeEndpoint { get; set; }

        public string TestLogoutRequest { get; set; }

        public override string CreateAuthenticationRequestUrl()
        {
            return TestAuthorizeEndpoint ?? base.CreateAuthenticationRequestUrl();
        }

        public override string CreateLogoutRequestUrl()
        {
            return TestLogoutRequest ?? base.CreateLogoutRequestUrl();
        }
    }
}
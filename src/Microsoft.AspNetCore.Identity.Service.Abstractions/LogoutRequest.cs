// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Microsoft.AspNetCore.Identity.Service
{
    public class LogoutRequest
    {
        private LogoutRequest(OpenIdConnectMessage message)
        {
            Message = message;
            IsValid = false;
        }

        private LogoutRequest(OpenIdConnectMessage message, string logoutRedirectUri)
        {
            Message = message;
            LogoutRedirectUri = logoutRedirectUri;
            IsValid = true;
        }

        public OpenIdConnectMessage Message { get; }
        public string LogoutRedirectUri { get; set; }
        public bool IsValid { get; }

        public static LogoutRequest Valid(OpenIdConnectMessage message, string logoutRedirectUri) => new LogoutRequest(message, logoutRedirectUri);
        public static LogoutRequest Invalid(OpenIdConnectMessage error) => new LogoutRequest(error);
    }
}

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Microsoft.AspNetCore.Identity.Service
{
    public class RedirectUriResolutionResult
    {
        private RedirectUriResolutionResult(string uri)
        {
            IsValid = true;
            Uri = uri;
        }

        private RedirectUriResolutionResult(OpenIdConnectMessage error)
        {
            IsValid = false;
            Error = error;
        }

        public bool IsValid { get; }
        public string Uri { get; }
        public OpenIdConnectMessage Error { get; }

        public static RedirectUriResolutionResult Valid(string uri)
        {
            return new RedirectUriResolutionResult(uri);
        }

        public static RedirectUriResolutionResult Invalid(OpenIdConnectMessage error)
        {
            return new RedirectUriResolutionResult(error);
        }
    }
}

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Microsoft.AspNetCore.Identity.Service
{
    public class ScopeResolutionResult
    {
        public ScopeResolutionResult(IEnumerable<ApplicationScope> scopes)
        {
            IsValid = true;
            Scopes = scopes;
        }

        public ScopeResolutionResult(OpenIdConnectMessage error)
        {
            IsValid = false;
            Error = error;
        }

        public IEnumerable<ApplicationScope> Scopes { get; }
        public OpenIdConnectMessage Error { get; }
        public bool IsValid { get; }

        public static ScopeResolutionResult Valid(IEnumerable<ApplicationScope> scopes)
        {
            return new ScopeResolutionResult(scopes);
        }

        public static ScopeResolutionResult Invalid(OpenIdConnectMessage error)
        {
            return new ScopeResolutionResult(error);
        }
    }
}

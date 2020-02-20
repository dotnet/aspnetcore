// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Authentication.WebAssembly.Msal
{
    public class MsalAuthenticationOptions
    {
        public string ClientId { get; set; }

        public string Authority { get; set; }

        public bool ValidateAuthority { get; set; } = true;

        public string RedirectUri { get; set; }

        public string PostLogoutRedirectUri { get; set; }

        public bool NavigateToLoginRequestUrl { get; set; } = false;
    }
}

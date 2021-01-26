// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.ApiAuthorization.IdentityServer
{
    internal class ClientDefinition : ServiceDefinition
    {
        public string RedirectUri { get; set; }
        public string LogoutUri { get; set; }
        public string ClientSecret { get; set; }
    }
}

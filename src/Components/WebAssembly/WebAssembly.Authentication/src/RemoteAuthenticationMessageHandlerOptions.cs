// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Components.WebAssembly.Authentication
{
    public class RemoteAuthenticationMessageHandlerOptions
    {
        public ISet<Uri> AllowedOrigins { get; set; } = new HashSet<Uri>();

        public AccessTokenRequestOptions TokenRequestOptions { get; set; }
    }
}

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Diagnostics.Identity.Service
{
    public class DeveloperCertificateOptions
    {
        public PathString ListeningEndpoint { get; set; } = "/tfp/IdentityService/signinsignup/oauth2/v2.0/authorize";
    }
}

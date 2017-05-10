// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Specialized;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Identity.Service
{
    public class ConfigurationContext
    {
        public string Id { get; set; }
        public HttpContext HttpContext { get; set; }
        public string AuthorizationEndpoint { get; set; }
        public string TokenEndpoint { get; set; }
        public string JwksUriEndpoint { get; set; }
        public string EndSessionEndpoint { get; set; }
        public NameValueCollection AdditionalValues { get; set; }
    }
}

// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Security.Claims;
using Microsoft.AspNet.HttpFeature.Security;

namespace Microsoft.AspNet.PipelineCore.Security
{
    public class HttpAuthenticationFeature : IHttpAuthenticationFeature
    {
        public HttpAuthenticationFeature()
        {
        }

        public ClaimsPrincipal User
        {
            get;
            set;
        }

        public IAuthenticationHandler Handler
        {
            get;
            set;
        }
    }
}

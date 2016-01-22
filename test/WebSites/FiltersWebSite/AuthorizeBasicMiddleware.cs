// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace FiltersWebSite
{
    public class AuthorizeBasicMiddleware : AuthenticationMiddleware<BasicOptions>
    {
        public AuthorizeBasicMiddleware(
            RequestDelegate next,
            ILoggerFactory loggerFactory,
            UrlEncoder encoder,
            string authScheme) :
                base(next,
                     new BasicOptions { AuthenticationScheme = authScheme },
                     loggerFactory,
                     encoder)
        {
        }

        protected override AuthenticationHandler<BasicOptions> CreateHandler()
        {
            return new BasicAuthenticationHandler();
        }
    }
}
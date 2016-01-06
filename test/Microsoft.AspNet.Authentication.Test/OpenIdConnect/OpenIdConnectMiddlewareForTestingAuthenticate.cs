// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text.Encodings.Web;
using Microsoft.AspNet.Authentication.OpenIdConnect;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.DataProtection;
using Microsoft.AspNet.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNet.Authentication.Tests.OpenIdConnect
{

    /// <summary>
    /// pass a <see cref="OpenIdConnectHandler"/> as the AuthenticationHandler
    /// configured to handle certain messages.
    /// </summary>
    public class OpenIdConnectMiddlewareForTestingAuthenticate : OpenIdConnectMiddleware
    {
        OpenIdConnectHandler _handler;

        public OpenIdConnectMiddlewareForTestingAuthenticate(
            RequestDelegate next,
            IDataProtectionProvider dataProtectionProvider,
            ILoggerFactory loggerFactory,
            UrlEncoder encoder,
            IServiceProvider services,
            IOptions<SharedAuthenticationOptions> sharedOptions,
            IOptions<OpenIdConnectOptions> options,
            HtmlEncoder htmlEncoder,
            OpenIdConnectHandler handler = null
            )
        : base(next, dataProtectionProvider, loggerFactory, encoder, services, sharedOptions, options, htmlEncoder)
        {
            _handler = handler;
        }

        protected override AuthenticationHandler<OpenIdConnectOptions> CreateHandler()
        {
            return _handler ?? base.CreateHandler();
        }
    }
}

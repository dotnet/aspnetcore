// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Authentication.OpenIdConnect;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.DataProtection;
using Microsoft.Framework.Logging;
using Microsoft.Framework.OptionsModel;
using Microsoft.Framework.WebEncoders;

namespace Microsoft.AspNet.Authentication.Tests.OpenIdConnect
{

    /// <summary>
    /// pass a <see cref="OpenIdConnectAuthenticationHandler"/> as the AuthenticationHandler
    /// configured to handle certain messages.
    /// </summary>
    public class OpenIdConnectAuthenticationMiddlewareForTestingAuthenticate : OpenIdConnectAuthenticationMiddleware
    {
        OpenIdConnectAuthenticationHandler _handler;

        public OpenIdConnectAuthenticationMiddlewareForTestingAuthenticate(
            RequestDelegate next,            
            IDataProtectionProvider dataProtectionProvider,
            ILoggerFactory loggerFactory,
            IUrlEncoder encoder,
            IServiceProvider services,
            IOptions<SharedAuthenticationOptions> sharedOptions,
            IOptions<OpenIdConnectAuthenticationOptions> options,
            ConfigureOptions<OpenIdConnectAuthenticationOptions> configureOptions = null,
            OpenIdConnectAuthenticationHandler handler = null
            )
        : base(next, dataProtectionProvider, loggerFactory, encoder, services, sharedOptions, options, configureOptions)
        {
            _handler = handler;
            var customFactory = loggerFactory as InMemoryLoggerFactory;
            if (customFactory != null)
                Logger = customFactory.Logger;
        }

        protected override AuthenticationHandler<OpenIdConnectAuthenticationOptions> CreateHandler()
        {
            return _handler ?? base.CreateHandler();
        }
    }
}

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Authentication
{
    /// <summary>
    /// Implementation of <see cref="IAuthenticationHandlerProvider"/>.
    /// </summary>
    public class AuthenticationHandlerProvider : IAuthenticationHandlerProvider
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="schemes">The <see cref="IAuthenticationHandlerProvider"/>.</param>
        public AuthenticationHandlerProvider(IAuthenticationSchemeProvider schemes)
        {
            Schemes = schemes;
        }

        /// <summary>
        /// The <see cref="IAuthenticationHandlerProvider"/>.
        /// </summary>
        public IAuthenticationSchemeProvider Schemes { get; }

        // handler instance cache, need to initialize once per request
        private Dictionary<string, IAuthenticationHandler> _handlerMap = new Dictionary<string, IAuthenticationHandler>(StringComparer.Ordinal);

        /// <summary>
        /// Returns the handler instance that will be used.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="authenticationScheme">The name of the authentication scheme being handled.</param>
        /// <returns>The handler instance.</returns>
        public async Task<IAuthenticationHandler> GetHandlerAsync(HttpContext context, string authenticationScheme)
        {
            if (_handlerMap.ContainsKey(authenticationScheme))
            {
                return _handlerMap[authenticationScheme];
            }

            var scheme = await Schemes.GetSchemeAsync(authenticationScheme);
            if (scheme == null)
            {
                return null;
            }
            var handler = (context.RequestServices.GetService(scheme.HandlerType) ??
                ActivatorUtilities.CreateInstance(context.RequestServices, scheme.HandlerType))
                as IAuthenticationHandler;
            if (handler != null)
            {
                await handler.InitializeAsync(scheme, context);
                _handlerMap[authenticationScheme] = handler;
            }
            return handler;
        }
    }
}

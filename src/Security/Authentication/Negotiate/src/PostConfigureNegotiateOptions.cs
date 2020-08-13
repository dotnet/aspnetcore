// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Authentication.Negotiate
{
    /// <summary>
    /// Reconfigures the NegotiateOptions to defer to the integrated server authentication if present.
    /// </summary>
    public class PostConfigureNegotiateOptions : IPostConfigureOptions<NegotiateOptions>
    {
        private readonly IServerIntegratedAuth _serverAuth;
        private readonly ILogger<NegotiateHandler> _logger;

        /// <summary>
        /// Creates a new <see cref="PostConfigureNegotiateOptions"/>
        /// </summary>
        /// <param name="serverAuthServices"></param>
        /// <param name="logger"></param>
        public PostConfigureNegotiateOptions(IEnumerable<IServerIntegratedAuth> serverAuthServices, ILogger<NegotiateHandler> logger)
        {
            _serverAuth = serverAuthServices.LastOrDefault();
            _logger = logger;
        }

        /// <summary>
        /// Invoked to post configure a TOptions instance.
        /// </summary>
        /// <param name="name">The name of the options instance being configured.</param>
        /// <param name="options">The options instance to configure.</param>
        public void PostConfigure(string name, NegotiateOptions options)
        {
            // If the server supports integrated authentication...
            if (_serverAuth != null)
            {
                // And it's on...
                if (_serverAuth.IsEnabled)
                {
                    // Forward to the server if something else wasn't already configured.
                    if (options.ForwardDefault == null)
                    {
                        Debug.Assert(_serverAuth.AuthenticationScheme != null);
                        options.ForwardDefault = _serverAuth.AuthenticationScheme;
                        options.DeferToServer = true;
                        _logger.Deferring();
                    }
                }
                // Otherwise fail, you shouldn't be using this auth handler on a server that supports integrated auth.
                else
                {
                    throw new InvalidOperationException("The Negotiate Authentication handler cannot be used on a server that directly supports Windows Authentication."
                        + " Enable Windows Authentication for the server and the Negotiate Authentication handler will defer to it.");
                }
            }
        }
    }
}

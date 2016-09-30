// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Server.WebListener.Internal
{
    public class WebListenerOptionsSetup : IConfigureOptions<WebListenerOptions>
    {
        private ILoggerFactory _loggerFactory;

        public WebListenerOptionsSetup(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
        }

        public void Configure(WebListenerOptions options)
        {
            options.ListenerSettings.Logger = _loggerFactory.CreateLogger<Microsoft.Net.Http.Server.WebListener>();
        }
    }
}
// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Components.WebView.Hosting
{
    public class WebViewHost
    {
        private Dictionary<IRenderPort, IServiceScope> _clientRenderers = new();

        public IServiceProvider Services { get; set; }

        public void AttachRenderClient(IRenderPort renderClient)
        {
            var factory = Services.GetRequiredService<IServiceScopeFactory>();
            var scope = factory.CreateScope();
            _clientRenderers.Add(renderClient, scope);
            renderClient.Attach(scope.ServiceProvider);
        }
    }
}

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.IISIntegration;

namespace AspnetCoreModule.TestSites.Standard
{
    internal class IISSetupFilter : IStartupFilter
    {
        private readonly string _pairingToken;
        
        internal IISSetupFilter(string pairingToken)
        {
            _pairingToken = pairingToken;
        }

        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return app =>
            {
                app.UseMiddleware<TestMiddleWareBeforeIISMiddleWare>();
                app.UseMiddleware<IISMiddleware>(_pairingToken);
                next(app);
            };
        }
    }
}
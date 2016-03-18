// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Microsoft.AspNetCore.Server.IISIntegration
{
    internal class IISSetupFilter : IStartupFilter
    {
        private string _pairingToken;

        internal IISSetupFilter(string pairingToken)
        {
            _pairingToken = pairingToken;
        }

        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return app =>
            {
                app.UseForwardedHeaders();
                app.UseMiddleware<IISMiddleware>(_pairingToken);
                next(app);
            };
        }
    }
}

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Server.IISIntegration
{
    internal class IISSetupFilter : IStartupFilter
    {
        private readonly string _pairingToken;
        private readonly PathString _pathBase;

        internal IISSetupFilter(string pairingToken, PathString pathBase)
        {
            _pairingToken = pairingToken;
            _pathBase = pathBase;
        }

        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return app =>
            {
                app.UsePathBase(_pathBase);
                app.UseForwardedHeaders();
                app.UseMiddleware<IISMiddleware>(_pairingToken);
                next(app);
            };
        }
    }
}

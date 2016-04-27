// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Builder;

namespace Microsoft.AspNetCore.Hosting
{
    public class DelegateStartup : StartupBase
    {
        private Action<IApplicationBuilder> _configureApp;
        
        public DelegateStartup(Action<IApplicationBuilder> configureApp)
        {
            _configureApp = configureApp;
        }

        public override void Configure(IApplicationBuilder app) => _configureApp(app);
    }
}
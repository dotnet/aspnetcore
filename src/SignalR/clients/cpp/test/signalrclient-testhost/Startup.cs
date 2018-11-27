// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Owin;

namespace SelfHost
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.Map("/raw-connection", map => map.RunSignalR<RawConnection>());

            app.MapSignalR();

            app.Map("/custom", map => map.RunSignalR());
        }
    }
}
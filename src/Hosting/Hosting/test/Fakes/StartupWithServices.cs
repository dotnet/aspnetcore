// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;

namespace Microsoft.AspNetCore.Hosting.Fakes
{
    public class StartupWithServices
    {
        private readonly IFakeStartupCallback _fakeStartupCallback;

        public StartupWithServices(IFakeStartupCallback fakeStartupCallback)
        {
            _fakeStartupCallback = fakeStartupCallback;
        }

        public void Configure(IApplicationBuilder builder, IFakeStartupCallback fakeStartupCallback2)
        {
            _fakeStartupCallback.ConfigurationMethodCalled(this);
            fakeStartupCallback2.ConfigurationMethodCalled(this);
        }
    }
}
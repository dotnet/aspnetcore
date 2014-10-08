// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Builder;

namespace Microsoft.AspNet.Hosting.Fakes
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
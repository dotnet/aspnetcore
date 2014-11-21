// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Hosting
{
    public class ConfigureHostingEnvironment : IConfigureHostingEnvironment
    {
        private readonly Action<IHostingEnvironment> _action;

        public ConfigureHostingEnvironment(Action<IHostingEnvironment> configure)
        {
            _action = configure;
        }

        public void Configure(IHostingEnvironment hostingEnv)
        {
            _action.Invoke(hostingEnv);
        }
    }
}
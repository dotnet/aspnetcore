// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using System;
using Microsoft.Framework.ConfigurationModel;

namespace Microsoft.AspNet.Hosting
{
    internal class ConfigureHostingEnvironment : IConfigureHostingEnvironment
    {
        private IConfiguration _config;
        private const string EnvironmentKey = "ASPNET_ENV";

        public ConfigureHostingEnvironment(IConfiguration config)
        {
            _config = config;
        }

        public void Configure(IHostingEnvironment hostingEnv)
        {
            hostingEnv.EnvironmentName = _config?.Get(EnvironmentKey) ?? hostingEnv.EnvironmentName;
        }
    }
}
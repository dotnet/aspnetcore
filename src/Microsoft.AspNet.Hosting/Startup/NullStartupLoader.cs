// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Hosting.Startup
{
    public class NullStartupLoader : IStartupLoader
    {
        static NullStartupLoader()
        {
            Instance = new NullStartupLoader();
        }

        public static IStartupLoader Instance { get; private set; }

        public Action<IApplicationBuilder> LoadStartup(
            string applicationName, 
            string environmentName,
            IList<string> diagnosticMessages)
        {
            return null;
        }
    }
}
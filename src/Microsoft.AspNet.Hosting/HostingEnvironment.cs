// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Framework.Runtime;

namespace Microsoft.AspNet.Hosting
{
    public class HostingEnvironment : IHostingEnvironment
    {
        private const string DefaultEnvironmentName = "Development";

        public HostingEnvironment(IApplicationEnvironment appEnv, IEnumerable<IConfigureHostingEnvironment> configures)
        {
            WebRoot = HostingUtilities.GetWebRoot(appEnv.ApplicationBasePath);
            EnvironmentName = DefaultEnvironmentName;
            foreach (var configure in configures)
            {
                configure.Configure(this);
            }
        }

        public string EnvironmentName { get; set; }

        public string WebRoot { get; set; }
    }
}
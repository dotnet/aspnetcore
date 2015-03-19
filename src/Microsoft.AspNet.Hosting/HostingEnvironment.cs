// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.FileProviders;
using Microsoft.AspNet.Hosting.Internal;
using Microsoft.Framework.Runtime;

namespace Microsoft.AspNet.Hosting
{
    public class HostingEnvironment : IHostingEnvironment
    {
        internal const string DefaultEnvironmentName = "Development";

        public HostingEnvironment(IApplicationEnvironment appEnvironment)
        {
            EnvironmentName = DefaultEnvironmentName;
            WebRootPath = HostingUtilities.GetWebRoot(appEnvironment.ApplicationBasePath);
            WebRootFileProvider = new PhysicalFileProvider(WebRootPath);
        }

        public string EnvironmentName { get; set; }

        public string WebRootPath { get; set; }

        public IFileProvider WebRootFileProvider { get; private set; }
    }
}
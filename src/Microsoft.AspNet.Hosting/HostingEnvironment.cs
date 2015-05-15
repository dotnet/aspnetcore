// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.FileProviders;
using Microsoft.AspNet.Hosting.Internal;

namespace Microsoft.AspNet.Hosting
{
    public class HostingEnvironment : IHostingEnvironment
    {
        internal const string DefaultEnvironmentName = "Production";

        public string EnvironmentName { get; set; } = DefaultEnvironmentName;

        public string WebRootPath { get; set; }

        public IFileProvider WebRootFileProvider { get; set; }
    }
}
// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.Hosting
{
#pragma warning disable CS0618 // Type or member is obsolete
    internal class HostingEnvironment : IHostingEnvironment, Extensions.Hosting.IHostingEnvironment, IWebHostEnvironment
#pragma warning restore CS0618 // Type or member is obsolete
    {
        public string EnvironmentName { get; set; } = Extensions.Hosting.Environments.Production;

        public string ApplicationName { get; set; } = default!;

        public string WebRootPath { get; set; } = default!;

        public IFileProvider WebRootFileProvider { get; set; } = default!;

        public string ContentRootPath { get; set; } = default!;

        public IFileProvider ContentRootFileProvider { get; set; } = default!;
    }
}

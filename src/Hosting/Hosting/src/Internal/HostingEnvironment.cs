// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

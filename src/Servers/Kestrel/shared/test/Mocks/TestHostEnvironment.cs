// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests;

internal sealed class TestHostEnvironment : IHostEnvironment
{
    public string EnvironmentName { get; set; } = Environments.Production;

    public string ApplicationName { get; set; } = "TestApplication";

    public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();

    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}

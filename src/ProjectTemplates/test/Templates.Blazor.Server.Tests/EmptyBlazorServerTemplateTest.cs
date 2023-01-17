// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing;
using Templates.Test.Helpers;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Templates.Blazor.Test;

public class EmptyBlazorServerTemplateTest : BlazorTemplateTest
{
    public EmptyBlazorServerTemplateTest(ProjectFactoryFixture projectFactory)
        : base(projectFactory)
    {
    }

    public override string ProjectType { get; } = "blazorserver-empty";

    [Fact]
    public Task EmptyBlazorServerTemplateWorks() => CreateBuildPublishAsync();

    [Fact]
    public Task EmptyBlazorServerTemplate_NoHttps_Works() => CreateBuildPublishAsync(args: new[] { ArgConstants.NoHttps });
}

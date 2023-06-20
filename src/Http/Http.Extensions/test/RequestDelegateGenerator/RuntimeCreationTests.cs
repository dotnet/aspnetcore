// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Text;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
namespace Microsoft.AspNetCore.Http.Generators.Tests;

public partial class RuntimeCreationTests : RequestDelegateCreationTests
{
    protected override bool IsGeneratorEnabled { get; } = false;

    [Theory]
    [InlineData("BindAsyncWrongType")]
    [InlineData("BindAsyncFromStaticAbstractInterfaceWrongType")]
    [InlineData("InheritBindAsyncWrongType")]
    public async Task MapAction_BindAsync_WithWrongType_IsNotUsed(string bindAsyncType)
    {
        var source = $$"""
app.MapGet("/", ({{bindAsyncType}} myNotBindAsyncParam) => { });
""";
        var (result, compilation) = await RunGeneratorAsync(source);

        var ex = Assert.Throws<InvalidOperationException>(() => GetEndpointFromCompilation(compilation));
        Assert.StartsWith($"BindAsync method found on {bindAsyncType} with incorrect format.", ex.Message);
    }
}

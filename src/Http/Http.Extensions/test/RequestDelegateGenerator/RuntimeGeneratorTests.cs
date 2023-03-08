// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.CodeAnalysis;
namespace Microsoft.AspNetCore.Http.Generators.Tests;

public class RuntimeGeneratorTests : RequestDelegateGeneratorTests
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

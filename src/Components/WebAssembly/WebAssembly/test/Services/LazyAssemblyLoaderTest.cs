// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.AspNetCore.Components.WebAssembly.Infrastructure;
using Microsoft.JSInterop;
using Moq;

namespace Microsoft.AspNetCore.Components.WebAssembly.Services;

public class LazyAssemblyLoaderTest
{
    [Fact]
    public void GetRequiredAssemblyNames_IncludesParameterAssemblies()
    {
        var marker = new ComponentMarker();
        marker.WriteWebAssemblyData("RootAssembly", "RootComponent", JsonSerializer.Serialize(
            new ComponentParameter[]
            {
                new() { Name = "Value", Assembly = "ParameterAssembly", TypeName = "Payload" },
                new() { Name = "NullValue" },
            }, WebAssemblyJsonSerializerContext.Default.ComponentParameterArray), "[]");

        Assert.Equal(["RootAssembly", "ParameterAssembly"], LazyAssemblyLoader.GetRequiredAssemblyNames(marker));
    }

    [Theory]
    [InlineData("Microsoft.AspNetCore.Components.Endpoints", "Microsoft.AspNetCore.Components.SerializedRenderFragment", false)]
    [InlineData("OtherAssembly", "Microsoft.AspNetCore.Components.SerializedRenderFragment", true)]
    [InlineData("Microsoft.AspNetCore.Components.Endpoints", "OtherType", true)]
    public void GetRequiredAssemblyNames_ExcludesOnlySerializedRenderFragmentSentinel(string assembly, string typeName, bool includeParameterAssembly)
    {
        var marker = new ComponentMarker();
        marker.WriteWebAssemblyData("RootAssembly", "RootComponent", JsonSerializer.Serialize(
            new ComponentParameter[] { new() { Name = "Value", Assembly = assembly, TypeName = typeName } },
            WebAssemblyJsonSerializerContext.Default.ComponentParameterArray), "[]");

        var expected = includeParameterAssembly ? new[] { "RootAssembly", assembly } : ["RootAssembly"];
        Assert.Equal(expected, LazyAssemblyLoader.GetRequiredAssemblyNames(marker));
    }

    [Fact]
    public async Task LoadMissingAssembliesAsync_UnknownAssembly_PreservesServerError()
    {
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateLoader().LoadMissingAssembliesAsync(["NonExistent.LazyAssembly"]));

        Assert.Contains("NonExistent.LazyAssembly", exception.Message);
    }

    private static LazyAssemblyLoader CreateLoader()
        => new(Mock.Of<IJSRuntime>());

    [Fact]
    public async Task LoadMissingAssembliesAsync_AlreadyLoadedAssembly_IsFilteredOut()
    {
        var loader = CreateLoader();

        // System.Private.CoreLib is always in AppDomain; the method must not attempt to reload it.
        var alreadyLoadedName = typeof(object).Assembly.GetName().Name!;

        // Would throw FileNotFoundException if it tried to re-load a non-lazy assembly via LoadAssembliesAsync.
        await loader.LoadMissingAssembliesAsync([alreadyLoadedName]);
    }
}
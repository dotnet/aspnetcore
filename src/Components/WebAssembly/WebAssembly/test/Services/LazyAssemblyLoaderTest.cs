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

    [Fact]
    public void GetRequiredAssemblyNames_IncludesGenericArgumentAssembly()
    {
        var marker = new ComponentMarker();
        marker.WriteWebAssemblyData("RootAssembly", "RootComponent", JsonSerializer.Serialize(
            new ComponentParameter[]
            {
                new() { Name = "Value", Assembly = "System.Collections", TypeName = "System.Collections.Generic.List`1[[Payload, LazyPayload, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]" },
            }, WebAssemblyJsonSerializerContext.Default.ComponentParameterArray), "[]");

        Assert.Equal(["RootAssembly", "System.Collections", "LazyPayload"], LazyAssemblyLoader.GetRequiredAssemblyNames(marker));
    }

    [Theory]
    [InlineData("Pair`2[[First, FirstAssembly],[Second, SecondAssembly]]", new[] { "FirstAssembly", "SecondAssembly" })]
    [InlineData("Outer`1[[Inner`1[[Payload, PayloadAssembly]], InnerAssembly]]", new[] { "InnerAssembly", "PayloadAssembly" })]
    [InlineData("Outer`1[[Payload[], PayloadAssembly]][,][]", new[] { "PayloadAssembly" })]
    [InlineData("Outer`1[[Inner`1[[Payload, PayloadAssembly]][], InnerAssembly]]", new[] { "InnerAssembly", "PayloadAssembly" })]
    [InlineData("Outer`1[Inner`1[[Payload, PayloadAssembly]]]", new[] { "PayloadAssembly" })]
    [InlineData("Pair`2[[Payload, PayloadAssembly],[Other, PayloadAssembly]]", new[] { "PayloadAssembly", "PayloadAssembly" })]
    [InlineData(@"Outer`1[[Namespace.Escaped\+Name, PayloadAssembly]]", new[] { "PayloadAssembly" })]
    [InlineData("Payload", new string[0])]
    [InlineData("Payload[]", new string[0])]
    [InlineData("Outer`1", new string[0])]
    [InlineData("Outer`1[Payload]", new string[0])]
    [InlineData("Outer`1[[Payload, PayloadAssembly]", new string[0])]
    [InlineData("Outer`1[[Payload, PayloadAssembly, Version=invalid]]", new string[0])]
    [InlineData(null, new string[0])]
    public void GetRequiredAssemblyNames_DiscoversGenericArgumentsWithoutResolvingTypes(string typeName, string[] argumentAssemblies)
    {
        var marker = new ComponentMarker();
        marker.WriteWebAssemblyData("RootAssembly", "RootComponent", JsonSerializer.Serialize(
            new ComponentParameter[] { new() { Name = "Value", Assembly = "ParameterAssembly", TypeName = typeName } },
            WebAssemblyJsonSerializerContext.Default.ComponentParameterArray), "[]");

        Assert.Equal(new[] { "RootAssembly", "ParameterAssembly" }.Concat(argumentAssemblies), LazyAssemblyLoader.GetRequiredAssemblyNames(marker));
    }

    [Fact]
    public void GetRequiredAssemblyNames_UsesRuntimeGenericTypeName()
    {
        var parameterType = typeof(Dictionary<string, LazyAssemblyLoaderTest[]>);
        var marker = new ComponentMarker();
        marker.WriteWebAssemblyData("RootAssembly", "RootComponent", JsonSerializer.Serialize(
            new ComponentParameter[]
            {
                new() { Name = "Value", Assembly = parameterType.Assembly.GetName().Name, TypeName = parameterType.FullName },
            }, WebAssemblyJsonSerializerContext.Default.ComponentParameterArray), "[]");

        Assert.Equal(
            new[] { "RootAssembly", parameterType.Assembly.GetName().Name, typeof(string).Assembly.GetName().Name, typeof(LazyAssemblyLoaderTest).Assembly.GetName().Name },
            LazyAssemblyLoader.GetRequiredAssemblyNames(marker));
    }

    [Fact]
    public void GetRequiredAssemblyNames_OverDefaultParserLimit_IncludesGenericArgumentAssemblies()
    {
        var typeName = "Payload";
        for (var i = 0; i < 32; i++)
        {
            typeName = $"Outer`1[[{typeName}, ArgumentAssembly]]";
        }

        var marker = new ComponentMarker();
        marker.WriteWebAssemblyData("RootAssembly", "RootComponent", JsonSerializer.Serialize(
            new ComponentParameter[] { new() { Name = "Value", Assembly = "ParameterAssembly", TypeName = typeName } },
            WebAssemblyJsonSerializerContext.Default.ComponentParameterArray), "[]");

        Assert.Equal(new[] { "RootAssembly", "ParameterAssembly" }.Concat(Enumerable.Repeat("ArgumentAssembly", 32)), LazyAssemblyLoader.GetRequiredAssemblyNames(marker));
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
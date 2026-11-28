// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.AspNetCore.Components.WebAssembly.Infrastructure;
using Microsoft.AspNetCore.Components.WebAssembly.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Moq;

namespace Microsoft.AspNetCore.Components.WebAssembly.Hosting;

public class LazyLoadingRegistrationTest
{
    [Fact]
    public async Task ResolveDeferredRootComponentsAsync_MissingParameterAssembly_DefersUntilStartup()
    {
        var methods = new Mock<IInternalJSImportMethods>();
        methods.Setup(m => m.NavigationManager_GetBaseUri()).Returns("https://example.com/");
        methods.Setup(m => m.NavigationManager_GetLocationHref()).Returns("https://example.com/");
        methods.Setup(m => m.GetApplicationEnvironment()).Returns("Production");
        methods.Setup(m => m.GetAppSettingsFiles()).Returns([]);
        methods.Setup(m => m.RegisteredComponents_GetRegisteredComponentsCount()).Returns(1);
        methods.Setup(m => m.RegisteredComponents_GetAssembly(0)).Returns(typeof(ParameterComponent).Assembly.GetName().Name!);
        methods.Setup(m => m.RegisteredComponents_GetTypeName(0)).Returns(typeof(ParameterComponent).FullName!);
        methods.Setup(m => m.RegisteredComponents_GetParameterDefinitions(0)).Returns(JsonSerializer.Serialize(
            new ComponentParameter[]
            {
                new() { Name = nameof(ParameterComponent.Value), Assembly = "NonExistent.ParameterAssembly", TypeName = "Payload" },
            }, WebAssemblyJsonSerializerContext.Default.ComponentParameterArray));
        methods.Setup(m => m.RegisteredComponents_GetParameterValues(0)).Returns("[{}]");

        var builder = new WebAssemblyHostBuilder(methods.Object);
        Assert.Empty(builder.RootComponents);
        builder.Services.AddSingleton(Mock.Of<IJSRuntime>());
        await using var host = builder.Build();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => builder.ResolveDeferredRootComponentsAsync(host.Services));

        Assert.Contains("NonExistent.ParameterAssembly", exception.Message);
        Assert.Empty(builder.RootComponents);
    }

    private sealed class ParameterComponent : ComponentBase
    {
        [Parameter]
        public object Value { get; set; }
    }

    [Fact]
    public async Task ResolveDeferredRootComponentsAsync_WhenNothingDeferred_ReturnsImmediately()
    {
        // zero registered components means nothing is ever deferred.
        var builder = new WebAssemblyHostBuilder(new TestInternalJSImportMethods());
        builder.Services.AddSingleton(Mock.Of<IJSRuntime>());
        var host = builder.Build();

        // should complete without calling LazyAssemblyLoader at all.
        await builder.ResolveDeferredRootComponentsAsync(host.Services);

        // RootComponents still empty (nothing was deferred to add).
        Assert.Empty(builder.RootComponents);
    }

    [Fact]
    public void ResolveOperationDescriptors_RemoveOperationsOnly_ReturnsUnmodifiedBatch()
    {
        var batch = new RootComponentOperationBatch
        {
            BatchId = 42,
            Operations =
            [
                new RootComponentOperation { Type = RootComponentOperationType.Remove, SsrComponentId = 1 }
            ]
        };

        // Remove operations must be skipped; no type resolution attempted.
        var result = DefaultWebAssemblyJSRuntime.ResolveOperationDescriptors(batch);

        Assert.Same(batch, result);
        Assert.Null(batch.Operations[0].Descriptor);
    }

    [Fact]
    public void ResolveOperationDescriptors_UnknownAssembly_Throws()
    {
        var marker = new ComponentMarker();
        marker.WriteWebAssemblyData(
            "NonExistent.Assembly.ThatDoesNotExist",
            "NonExistent.Assembly.ThatDoesNotExist.SomeComponent",
            Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(
                Array.Empty<ComponentParameter>(),
                WebAssemblyJsonSerializerContext.Default.ComponentParameterArray)),
            Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(
                Array.Empty<ComponentParameter>(),
                WebAssemblyJsonSerializerContext.Default.ComponentParameterArray)));

        var batch = new RootComponentOperationBatch
        {
            BatchId = 1,
            Operations =
            [
                new RootComponentOperation { Type = RootComponentOperationType.Add, Marker = marker }
            ]
        };

        var ex = Assert.Throws<InvalidOperationException>(
            () => DefaultWebAssemblyJSRuntime.ResolveOperationDescriptors(batch));

        Assert.Contains("NonExistent.Assembly.ThatDoesNotExist.SomeComponent", ex.Message);
        Assert.Contains("NonExistent.Assembly.ThatDoesNotExist", ex.Message);
    }
}
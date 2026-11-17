// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Text.Json;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Web.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;

namespace Microsoft.AspNetCore.Components.Web;

public class WebRendererJsonResolverTest
{
    [Fact]
    public void OutOfProcessRendererAddsFrameworkInteropContracts()
    {
        var runtime = new CapturingJSRuntime();
        using var services = new ServiceCollection()
            .AddSingleton<IJSRuntime>(runtime)
            .BuildServiceProvider();

        using var renderer = new TestWebRenderer(services, runtime.Options);

        Assert.Same(WebRendererSerializerContext.Default, runtime.Options.TypeInfoResolverChain[0]);
        Assert.NotNull(runtime.Options.GetTypeInfo(typeof(NavigationOptions)));
        Assert.NotNull(runtime.Options.GetTypeInfo(typeof(float)));
        Assert.NotNull(runtime.Options.GetTypeInfo(typeof(long)));
        Assert.NotNull(runtime.Options.GetTypeInfo(typeof(JsonElement)));
        Assert.NotNull(runtime.Options.GetTypeInfo(typeof(ChangeEventArgs)));
        Assert.Equal("Blazor._internal.attachWebRendererInterop", runtime.LastIdentifier);
    }

    private sealed class CapturingJSRuntime : JSRuntime
    {
        public JsonSerializerOptions Options => JsonSerializerOptions;

        public string? LastIdentifier { get; private set; }

        protected override void BeginInvokeJS(
            long taskId,
            string identifier,
            string? argsJson,
            JSCallResultType resultType,
            long targetInstanceId)
        {
            LastIdentifier = identifier;
        }

        protected override void EndInvokeDotNet(
            DotNetInvocationInfo invocationInfo,
            in DotNetInvocationResult invocationResult)
        {
        }
    }

    private sealed class TestWebRenderer(
        IServiceProvider services,
        JsonSerializerOptions options)
        : WebRenderer(
            services,
            NullLoggerFactory.Instance,
            options,
            new JSComponentInterop(new JSComponentConfigurationStore()))
    {
        public override Dispatcher Dispatcher { get; } = Dispatcher.CreateDefault();

        protected override void AttachRootComponentToBrowser(int componentId, string domElementSelector)
        {
        }

        protected override void HandleException(Exception exception)
            => throw exception;

        protected override Task UpdateDisplayAsync(in RenderBatch renderBatch)
            => Task.CompletedTask;
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Web.Infrastructure;
using Microsoft.AspNetCore.Components.Web.Internal;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;

namespace Microsoft.AspNetCore.Components.Web;

public class WebRendererJsonResolverTest
{
    [Fact]
    public void OutOfProcessRendererPlacesApplicationContractsBeforeReflectionFallback()
    {
        var runtime = new CapturingJSRuntime();
        var applicationResolver = new NullResolver();
        using var services = CreateServices(runtime, applicationResolver);

        using var renderer = new TestWebRenderer(services, runtime.Options);

        Assert.Equal(5, runtime.Options.TypeInfoResolverChain.Count);
        Assert.Same(WebRendererSerializerContext.Default, runtime.Options.TypeInfoResolverChain[0]);
        Assert.Same(WebJSInteropSerializerContext.Default, runtime.Options.TypeInfoResolverChain[1]);
        Assert.Same(ConverterBackedTypeInfoResolver.Instance, runtime.Options.TypeInfoResolverChain[2]);
        Assert.Same(applicationResolver, runtime.Options.TypeInfoResolverChain[3]);
        Assert.IsType<DefaultJsonTypeInfoResolver>(runtime.Options.TypeInfoResolverChain[4]);
    }

    [ConditionalFact]
    [RemoteExecutionSupported]
    public void OutOfProcessRendererOrdersContractsWithoutDuplicatingExistingResolvers()
    {
        var remoteOptions = CreateReflectionDisabledOptions();

        using var remoteHandle = RemoteExecutor.Invoke(static () =>
        {
            var runtime = new CapturingJSRuntime();
            var applicationResolver = new NullResolver();
            runtime.Options.TypeInfoResolverChain.Add(WebJSInteropSerializerContext.Default);
            runtime.Options.TypeInfoResolverChain.Add(applicationResolver);
            using var services = CreateServices(runtime, applicationResolver);

            using var renderer = new TestWebRenderer(services, runtime.Options);

            Assert.Equal(4, runtime.Options.TypeInfoResolverChain.Count);
            Assert.Same(WebRendererSerializerContext.Default, runtime.Options.TypeInfoResolverChain[0]);
            Assert.Same(WebJSInteropSerializerContext.Default, runtime.Options.TypeInfoResolverChain[1]);
            Assert.Same(ConverterBackedTypeInfoResolver.Instance, runtime.Options.TypeInfoResolverChain[2]);
            Assert.Same(applicationResolver, runtime.Options.TypeInfoResolverChain[3]);
            Assert.Equal("Blazor._internal.attachWebRendererInterop", runtime.LastIdentifier);
            Assert.NotNull(runtime.LastArgsJson);
        }, remoteOptions);
    }

    [ConditionalFact]
    [RemoteExecutionSupported]
    public void OutOfProcessRendererDoesNotMutateReadOnlyOptions()
    {
        var remoteOptions = CreateReflectionDisabledOptions();

        using var remoteHandle = RemoteExecutor.Invoke(static () =>
        {
            var runtime = new CapturingJSRuntime();
            var applicationResolver = new NullResolver();
            runtime.Options.TypeInfoResolverChain.Add(applicationResolver);
            runtime.Options.TypeInfoResolverChain.Add(ConverterBackedTypeInfoResolver.Instance);
            runtime.Options.TypeInfoResolverChain.Add(WebJSInteropSerializerContext.Default);
            runtime.Options.TypeInfoResolverChain.Add(WebRendererSerializerContext.Default);
            runtime.Options.MakeReadOnly();
            using var services = CreateServices(runtime, applicationResolver);

            using var renderer = new TestWebRenderer(services, runtime.Options);

            Assert.Equal(4, runtime.Options.TypeInfoResolverChain.Count);
            Assert.Same(applicationResolver, runtime.Options.TypeInfoResolverChain[0]);
            Assert.Same(ConverterBackedTypeInfoResolver.Instance, runtime.Options.TypeInfoResolverChain[1]);
            Assert.Same(WebJSInteropSerializerContext.Default, runtime.Options.TypeInfoResolverChain[2]);
            Assert.Same(WebRendererSerializerContext.Default, runtime.Options.TypeInfoResolverChain[3]);
            Assert.Equal("Blazor._internal.attachWebRendererInterop", runtime.LastIdentifier);
            Assert.NotNull(runtime.LastArgsJson);
        }, remoteOptions);
    }

    private static RemoteInvokeOptions CreateReflectionDisabledOptions()
    {
        var options = new RemoteInvokeOptions();
        options.RuntimeConfigurationOptions.Add(
            "System.Text.Json.JsonSerializer.IsReflectionEnabledByDefault",
            false.ToString());
        return options;
    }

    private static ServiceProvider CreateServices(
        CapturingJSRuntime runtime,
        IJsonTypeInfoResolver applicationResolver)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IJSRuntime>(runtime);
        services.AddSingleton<IComponentJsonMetadataResolver>(
            new TestComponentJsonMetadataResolver(applicationResolver));
        return services.BuildServiceProvider();
    }

    private sealed class TestComponentJsonMetadataResolver(
        IJsonTypeInfoResolver resolver) : IComponentJsonMetadataResolver
    {
        public IJsonTypeInfoResolver JsonTypeInfoResolver => resolver;
    }

    private sealed class NullResolver : IJsonTypeInfoResolver
    {
        public JsonTypeInfo? GetTypeInfo(Type type, JsonSerializerOptions options)
            => null;
    }

    private sealed class CapturingJSRuntime : JSRuntime
    {
        public JsonSerializerOptions Options => JsonSerializerOptions;

        public string? LastIdentifier { get; private set; }

        public string? LastArgsJson { get; private set; }

        protected override void BeginInvokeJS(
            long taskId,
            string identifier,
            string? argsJson,
            JSCallResultType resultType,
            long targetInstanceId)
        {
            LastIdentifier = identifier;
            LastArgsJson = argsJson;
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

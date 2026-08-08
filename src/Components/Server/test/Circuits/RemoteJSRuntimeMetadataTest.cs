// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Components.Infrastructure;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;

namespace Microsoft.AspNetCore.Components.Server.Circuits;

[Experimental("ASPNETCORE9004", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
public class RemoteJSRuntimeMetadataTest
{
    [Fact]
    public void RegisteredContextsReachRemoteRuntimeDispatch()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions<CircuitOptions>();
        services.AddOptions<HubOptions<ComponentHub>>();
        services.AddComponentMetadata<FirstContext>();
        services.AddComponentMetadata<SecondContext>();
        services.AddScoped<IJSRuntime, RemoteJSRuntime>();
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var runtime = Assert.IsType<RemoteJSRuntime>(scope.ServiceProvider.GetRequiredService<IJSRuntime>());

        var first = DotNetDispatcher.Invoke(
            runtime,
            new DotNetInvocationInfo("TestAssembly", "first", default, default),
            "[]");
        var second = DotNetDispatcher.Invoke(
            runtime,
            new DotNetInvocationInfo("TestAssembly", "second", default, default),
            "[]");
        var shared = DotNetDispatcher.Invoke(
            runtime,
            new DotNetInvocationInfo("TestAssembly", "shared", default, default),
            "[]");

        Assert.Equal("\"first\"", first);
        Assert.Equal("\"second\"", second);
        Assert.Equal("\"first-shared\"", shared);
    }

    [Fact]
    public void ReflectionCompatibilityHandlesReceiverWithoutGeneratedCoverage()
    {
        var runtime = CreateRuntime<FirstContext>(out var provider, out var scope);
        using (provider)
        using (scope)
        {
            using var reference = DotNetObjectReference.Create<InteropBase>(new InaccessibleAnnotatedOverride());
            var objectId = TrackObjectReference(runtime, reference);

            var result = DotNetDispatcher.Invoke(
                runtime,
                new DotNetInvocationInfo(null, "virtual", objectId, default),
                "[]");

            Assert.Equal("\"inaccessible-derived\"", result);
        }
    }

    [Fact]
    public void ReflectionCompatibilityDetectsDuplicateForUncoveredNewSlot()
    {
        var runtime = CreateRuntime<FirstContext>(out var provider, out var scope);
        using (provider)
        using (scope)
        {
            using var reference = DotNetObjectReference.Create<InteropBase>(new InaccessibleAnnotatedNewSlot());
            var objectId = TrackObjectReference(runtime, reference);

            Assert.Throws<InvalidOperationException>(() => DotNetDispatcher.Invoke(
                runtime,
                new DotNetInvocationInfo(null, "inherited", objectId, default),
                "[]"));
        }
    }

    [ConditionalFact]
    [RemoteExecutionSupported]
    public void ReflectionResolutionCanBeDisabledWithoutDisablingGeneratedDescriptors()
    {
        var remoteOptions = new RemoteInvokeOptions();
        remoteOptions.RuntimeConfigurationOptions.Add(
            "Microsoft.JSInterop.JSInvokableMethodResolution.IsReflectionEnabledByDefault",
            false.ToString());

        using var remoteHandle = RemoteExecutor.Invoke(static () =>
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddOptions<CircuitOptions>();
            services.AddOptions<HubOptions<ComponentHub>>();
            services.AddComponentMetadata<FirstContext>();
            services.AddScoped<IJSRuntime, RemoteJSRuntime>();
            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            var runtime = Assert.IsType<RemoteJSRuntime>(scope.ServiceProvider.GetRequiredService<IJSRuntime>());

            var generated = DotNetDispatcher.Invoke(
                runtime,
                new DotNetInvocationInfo("TestAssembly", "first", default, default),
                "[]");

            Assert.Equal("\"first\"", generated);

            using var coveredReference = DotNetObjectReference.Create<InteropBase>(new CoveredDerived());
            var coveredObjectId = TrackObjectReference(runtime, coveredReference);
            Assert.Equal("\"inherited\"", DotNetDispatcher.Invoke(
                runtime,
                new DotNetInvocationInfo(null, "inherited", coveredObjectId, default),
                "[]"));

            using var uncoveredNewSlotReference = DotNetObjectReference.Create<InteropBase>(new InaccessibleAnnotatedNewSlot());
            var uncoveredNewSlotObjectId = TrackObjectReference(runtime, uncoveredNewSlotReference);
            Assert.Throws<ArgumentException>(() =>
            {
                DotNetDispatcher.Invoke(
                    runtime,
                    new DotNetInvocationInfo(null, "inherited", uncoveredNewSlotObjectId, default),
                    "[]");
            });

            using var uncoveredOverrideReference = DotNetObjectReference.Create<InteropBase>(new InaccessibleAnnotatedOverride());
            var uncoveredOverrideObjectId = TrackObjectReference(runtime, uncoveredOverrideReference);
            Assert.Throws<ArgumentException>(() =>
            {
                DotNetDispatcher.Invoke(
                    runtime,
                    new DotNetInvocationInfo(null, "virtual", uncoveredOverrideObjectId, default),
                    "[]");
            });

            Assert.Throws<ArgumentException>(() =>
            {
                DotNetDispatcher.Invoke(
                    runtime,
                    new DotNetInvocationInfo(
                        typeof(RemoteJSRuntimeMetadataTest).Assembly.GetName().Name,
                        nameof(ReflectionOnly),
                        default,
                        default),
                    "[]");
            });
        }, remoteOptions);
    }

    [ConditionalFact]
    [RemoteExecutionSupported]
    public void ReflectionResolutionDefaultsToEnabled()
    {
        using var remoteHandle = RemoteExecutor.Invoke(static () =>
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddOptions<CircuitOptions>();
            services.AddOptions<HubOptions<ComponentHub>>();
            services.AddScoped<IJSRuntime, RemoteJSRuntime>();
            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            var runtime = Assert.IsType<RemoteJSRuntime>(scope.ServiceProvider.GetRequiredService<IJSRuntime>());

            var reflected = DotNetDispatcher.Invoke(
                runtime,
                new DotNetInvocationInfo(
                    typeof(RemoteJSRuntimeMetadataTest).Assembly.GetName().Name,
                    nameof(ReflectionOnly),
                    default,
                    default),
                "[]");

            Assert.Equal("\"reflection\"", reflected);
        });
    }

    [JSInvokable]
    public static string ReflectionOnly() => "reflection";

    private static RemoteJSRuntime CreateRuntime<TContext>(
        out ServiceProvider provider,
        out IServiceScope scope)
        where TContext : RazorComponentsMetadataContext, new()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions<CircuitOptions>();
        services.AddOptions<HubOptions<ComponentHub>>();
        services.AddComponentMetadata<TContext>();
        services.AddScoped<IJSRuntime, RemoteJSRuntime>();
        provider = services.BuildServiceProvider();
        scope = provider.CreateScope();
        return Assert.IsType<RemoteJSRuntime>(scope.ServiceProvider.GetRequiredService<IJSRuntime>());
    }

    private static long TrackObjectReference<TValue>(
        RemoteJSRuntime runtime,
        DotNetObjectReference<TValue> reference)
        where TValue : class
    {
        using var document = JsonDocument.Parse(
            JsonSerializer.Serialize(reference, runtime.ReadJsonSerializerOptions()));
        return document.RootElement.GetProperty("__dotNetObject").GetInt64();
    }

    public class InteropBase
    {
        [JSInvokable("virtual")]
        public virtual string VirtualMethod() => "base";

        [JSInvokable("inherited")]
        public string InheritedMethod() => "inherited";
    }

    private sealed class InaccessibleAnnotatedOverride : InteropBase
    {
        [JSInvokable("virtual")]
        public override string VirtualMethod() => "inaccessible-derived";
    }

    private sealed class InaccessibleAnnotatedNewSlot : InteropBase
    {
        [JSInvokable("inherited")]
        public new string InheritedMethod() => "inaccessible-new-slot";
    }

    public sealed class CoveredDerived : InteropBase
    {
    }

    public sealed class FirstContext : TestContext
    {
        public override IReadOnlyList<JSInvokableMethodDescriptor> JSInvokableMethods
            =>
            [
                CreateDescriptor("first"),
                CreateDescriptor("shared", "shared-contribution", "first-shared"),
                CreateInstanceDescriptor("virtual", JSInvokableMethodKind.Override),
                CreateInstanceDescriptor("inherited", JSInvokableMethodKind.Method),
                CreateTypeCoverageDescriptor<CoveredDerived>(),
            ];
    }

    public sealed class SecondContext : TestContext
    {
        public override IReadOnlyList<JSInvokableMethodDescriptor> JSInvokableMethods
            =>
            [
                CreateDescriptor("shared", "shared-contribution", "second-shared"),
                CreateDescriptor("second"),
            ];
    }

    public abstract class TestContext : RazorComponentsMetadataContext
    {
        public override IReadOnlyList<Microsoft.AspNetCore.Components.Infrastructure.ComponentDescriptor> Components => [];

        public override IReadOnlyList<BindableTypeDescriptor> BindableTypes => [];

        public override IJsonTypeInfoResolver? JsonTypeInfoResolver => null;

        protected static JSInvokableMethodDescriptor CreateDescriptor(
            string identifier,
            string? methodKey = null,
            string? result = null)
            => new()
            {
                AssemblyName = "TestAssembly",
                TargetType = typeof(RemoteJSRuntimeMetadataTest),
                Identifier = identifier,
                IsStatic = true,
                MethodKey = methodKey,
                Invoke = (_, _, _) => ValueTask.FromResult<string?>($"\"{result ?? identifier}\""),
            };

        protected static JSInvokableMethodDescriptor CreateInstanceDescriptor(
            string identifier,
            JSInvokableMethodKind kind)
            => new()
            {
                AssemblyName = "TestAssembly",
                TargetType = typeof(InteropBase),
                Identifier = identifier,
                IsStatic = false,
                Kind = kind,
                Invoke = (target, _, _) => ValueTask.FromResult<string?>(
                    $"\"{(identifier == "virtual" ? ((InteropBase)target!).VirtualMethod() : ((InteropBase)target!).InheritedMethod())}\""),
            };

        protected static JSInvokableMethodDescriptor CreateTypeCoverageDescriptor<TTarget>()
            => new()
            {
                AssemblyName = "TestAssembly",
                TargetType = typeof(TTarget),
                Identifier = string.Empty,
                IsStatic = false,
                Kind = JSInvokableMethodKind.OverrideBlocker,
                Invoke = (_, _, _) => throw new InvalidOperationException(),
            };
    }
}

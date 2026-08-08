// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.JSInterop.Infrastructure;

namespace Microsoft.Extensions.DependencyInjection;

#pragma warning disable ASPNETCORE9004
public class ComponentJsonMetadataIsolationTest
{
    [ConditionalFact]
    [RemoteExecutionSupported]
    public void ExistingSerializerDoesNotObserveLaterContextRegistration()
    {
        var remoteOptions = new RemoteInvokeOptions();
        remoteOptions.RuntimeConfigurationOptions.Add(
            "System.Text.Json.JsonSerializer.IsReflectionEnabledByDefault",
            false.ToString());

        using var remoteHandle = RemoteExecutor.Invoke(static () =>
        {
            var services = new ServiceCollection();
            services.AddComponentMetadata<FirstContext>();
            using var firstProvider = services.BuildServiceProvider();
            var firstSerializer = new WebAssemblyComponentSerializer(firstProvider);

            services.AddComponentMetadata<SecondContext>();
            using var secondProvider = services.BuildServiceProvider();
            var secondSerializer = new WebAssemblyComponentSerializer(secondProvider);

            var firstMarker = ComponentMarker.Create(ComponentMarker.WebAssemblyMarkerType, prerendered: false, key: null);
            firstSerializer.SerializeInvocation(
                ref firstMarker,
                typeof(TestComponent),
                ParameterView.FromDictionary(new Dictionary<string, object?> { ["Value"] = new FirstPayload() }));

            var missingMarker = ComponentMarker.Create(ComponentMarker.WebAssemblyMarkerType, prerendered: false, key: null);
            void SerializeMissingContract() => firstSerializer.SerializeInvocation(
                    ref missingMarker,
                    typeof(TestComponent),
                    ParameterView.FromDictionary(new Dictionary<string, object?> { ["Value"] = new SecondPayload() }));
            Assert.Throws<NotSupportedException>((Action)SerializeMissingContract);

            var secondMarker = ComponentMarker.Create(ComponentMarker.WebAssemblyMarkerType, prerendered: false, key: null);
            secondSerializer.SerializeInvocation(
                ref secondMarker,
                typeof(TestComponent),
                ParameterView.FromDictionary(new Dictionary<string, object?> { ["Value"] = new SecondPayload() }));
        }, remoteOptions);
    }

    public sealed class FirstContext : RazorComponentsMetadataContext
    {
        public override IReadOnlyList<JSInvokableMethodDescriptor> JSInvokableMethods => [];

        public override IJsonTypeInfoResolver JsonTypeInfoResolver => FirstJsonContext.Default;
    }

    public sealed class SecondContext : RazorComponentsMetadataContext
    {
        public override IReadOnlyList<JSInvokableMethodDescriptor> JSInvokableMethods => [];

        public override IJsonTypeInfoResolver JsonTypeInfoResolver => SecondJsonContext.Default;
    }

    internal sealed class FirstPayload;

    internal sealed class SecondPayload;

    private sealed class TestComponent : IComponent
    {
        public void Attach(RenderHandle renderHandle)
        {
        }

        public Task SetParametersAsync(ParameterView parameters) => Task.CompletedTask;
    }
}

[JsonSerializable(typeof(ComponentJsonMetadataIsolationTest.FirstPayload))]
internal sealed partial class FirstJsonContext : JsonSerializerContext;

[JsonSerializable(typeof(ComponentJsonMetadataIsolationTest.SecondPayload))]
internal sealed partial class SecondJsonContext : JsonSerializerContext;
#pragma warning restore ASPNETCORE9004

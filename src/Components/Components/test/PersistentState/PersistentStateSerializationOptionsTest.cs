// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.DotNet.RemoteExecutor;

namespace Microsoft.AspNetCore.Components;

public partial class PersistentStateSerializationOptionsTest
{
    [ConditionalFact]
    [RemoteExecutionSupported]
    public void ResolverChainOrdersApplicationBeforeReflectionAndDeduplicates()
    {
        var remoteOptions = new RemoteInvokeOptions();
        remoteOptions.RuntimeConfigurationOptions.Add(
            "System.Text.Json.JsonSerializer.IsReflectionEnabledByDefault",
            true.ToString());

        using var remoteHandle = RemoteExecutor.Invoke(static () =>
        {
            var chain = PersistentStateSerializationOptions.Options.TypeInfoResolverChain;
            Assert.Equal(2, chain.Count);
            Assert.Same(PersistentStateJsonContext.Default, chain[0]);
            Assert.IsType<DefaultJsonTypeInfoResolver>(chain[1]);

            var applicationResolver = TestJsonContext.Default;
            PersistentStateSerializationOptions.AddResolver(applicationResolver);
            PersistentStateSerializationOptions.AddResolver(applicationResolver);

            Assert.Equal(3, chain.Count);
            Assert.Same(PersistentStateJsonContext.Default, chain[0]);
            Assert.Same(applicationResolver, chain[1]);
            Assert.IsType<DefaultJsonTypeInfoResolver>(chain[2]);
        }, remoteOptions);
    }

    [ConditionalFact]
    [RemoteExecutionSupported]
    public void ReflectionDisabledUsesGeneratedContractsAndSkipsUnsupportedTypes()
    {
        var remoteOptions = new RemoteInvokeOptions();
        remoteOptions.RuntimeConfigurationOptions.Add(
            "System.Text.Json.JsonSerializer.IsReflectionEnabledByDefault",
            false.ToString());

        using var remoteHandle = RemoteExecutor.Invoke(static () =>
        {
            Assert.False(JsonSerializer.IsReflectionEnabledByDefault);
            var chain = PersistentStateSerializationOptions.Options.TypeInfoResolverChain;
            Assert.Single(chain);
            Assert.Same(PersistentStateJsonContext.Default, chain[0]);
            Assert.True(PersistentStateSerializationOptions.CanSerialize(typeof(int)));

            PersistentStateSerializationOptions.AddResolver(TestJsonContext.Default);
            PersistentStateSerializationOptions.AddResolver(new ThrowingResolver());

            Assert.True(PersistentStateSerializationOptions.CanSerialize(typeof(SupportedPayload)));
            Assert.False(PersistentStateSerializationOptions.CanSerialize(typeof(MissingPayload)));
            Assert.False(PersistentStateSerializationOptions.CanSerialize(typeof(NotSupportedPayload)));
            Assert.False(PersistentStateSerializationOptions.CanSerialize(typeof(InvalidPayload)));
        }, remoteOptions);
    }

    private sealed class ThrowingResolver : IJsonTypeInfoResolver
    {
        public JsonTypeInfo? GetTypeInfo(Type type, JsonSerializerOptions options)
            => type == typeof(NotSupportedPayload)
                ? throw new NotSupportedException()
                : type == typeof(InvalidPayload)
                    ? throw new InvalidOperationException()
                    : null;
    }

    private sealed class SupportedPayload
    {
        public string? Value { get; set; }
    }

    private sealed class MissingPayload;

    private sealed class NotSupportedPayload;

    private sealed class InvalidPayload;

    [JsonSerializable(typeof(SupportedPayload))]
    private sealed partial class TestJsonContext : JsonSerializerContext;
}

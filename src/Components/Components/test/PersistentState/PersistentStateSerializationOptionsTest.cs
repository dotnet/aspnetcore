// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.DotNet.RemoteExecutor;

namespace Microsoft.AspNetCore.Components;

public class PersistentStateSerializationOptionsTest
{
    [Fact]
    public void CreateReturnsIndependentOrderedSnapshots()
    {
        var firstResolver = FirstJsonContext.Default;
        var secondResolver = SecondJsonContext.Default;

        var first = PersistentStateSerializationOptions.Create(firstResolver);
        var second = PersistentStateSerializationOptions.Create(secondResolver);

        Assert.NotSame(first, second);
        Assert.Same(PersistentStateJsonContext.Default, first.TypeInfoResolverChain[0]);
        Assert.Same(firstResolver, first.TypeInfoResolverChain[1]);
        Assert.IsType<DefaultJsonTypeInfoResolver>(first.TypeInfoResolverChain[2]);
        Assert.Same(PersistentStateJsonContext.Default, second.TypeInfoResolverChain[0]);
        Assert.Same(secondResolver, second.TypeInfoResolverChain[1]);
        Assert.IsType<DefaultJsonTypeInfoResolver>(second.TypeInfoResolverChain[2]);
    }

    [ConditionalFact]
    [RemoteExecutionSupported]
    public void ReflectionDisabledOmitsCompatibilityResolver()
    {
        var remoteOptions = new RemoteInvokeOptions();
        remoteOptions.RuntimeConfigurationOptions.Add(
            "System.Text.Json.JsonSerializer.IsReflectionEnabledByDefault",
            false.ToString());

        using var remoteHandle = RemoteExecutor.Invoke(static () =>
        {
            var options = PersistentStateSerializationOptions.Create(FirstJsonContext.Default);

            Assert.False(JsonSerializer.IsReflectionEnabledByDefault);
            Assert.Collection(
                options.TypeInfoResolverChain,
                resolver => Assert.Same(PersistentStateJsonContext.Default, resolver),
                resolver => Assert.Same(FirstJsonContext.Default, resolver));
        }, remoteOptions);
    }

    internal sealed class FirstPayload;

    internal sealed class SecondPayload;
}

[JsonSerializable(typeof(PersistentStateSerializationOptionsTest.FirstPayload))]
internal sealed partial class FirstJsonContext : JsonSerializerContext;

[JsonSerializable(typeof(PersistentStateSerializationOptionsTest.SecondPayload))]
internal sealed partial class SecondJsonContext : JsonSerializerContext;

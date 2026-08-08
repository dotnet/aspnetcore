// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.DotNet.RemoteExecutor;

namespace Microsoft.AspNetCore.Components.Endpoints;

public class ComponentSerializationSettingsTest
{
    [Fact]
    public void GeneratedMarkerContractsPrecedeReflectionFallback()
    {
        Assert.Collection(
            ServerComponentSerializationSettings.JsonSerializationOptions.TypeInfoResolverChain,
            resolver => Assert.Same(ServerComponentJsonContext.Default, resolver),
            resolver => Assert.IsType<DefaultJsonTypeInfoResolver>(resolver));
        Assert.Collection(
            WebAssemblyComponentSerializationSettings.JsonSerializationOptions.TypeInfoResolverChain,
            resolver => Assert.Same(WebAssemblyComponentJsonContext.Default, resolver),
            resolver => Assert.IsType<DefaultJsonTypeInfoResolver>(resolver));
    }

    [ConditionalFact]
    [RemoteExecutionSupported]
    public void ReflectionDisabledMarkerSettingsContainOnlyGeneratedContracts()
    {
        var options = new RemoteInvokeOptions();
        options.RuntimeConfigurationOptions.Add(
            "System.Text.Json.JsonSerializer.IsReflectionEnabledByDefault",
            false.ToString());

        using var remoteHandle = RemoteExecutor.Invoke(static () =>
        {
            Assert.False(JsonSerializer.IsReflectionEnabledByDefault);
            Assert.Same(
                ServerComponentJsonContext.Default,
                Assert.Single(ServerComponentSerializationSettings.JsonSerializationOptions.TypeInfoResolverChain));
            Assert.Same(
                WebAssemblyComponentJsonContext.Default,
                Assert.Single(WebAssemblyComponentSerializationSettings.JsonSerializationOptions.TypeInfoResolverChain));
        }, options);
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.DotNet.RemoteExecutor;

namespace Microsoft.AspNetCore.Http.Extensions;

public class JsonOptionsTests
{
    [ConditionalFact]
    [RemoteExecutionSupported]
    public void DefaultSerializerOptions_SetsTypeInfoResolverEmptyResolver_WhenJsonIsReflectionEnabledByDefaultFalse()
    {
        var options = new RemoteInvokeOptions();
        options.RuntimeConfigurationOptions.Add("System.Text.Json.JsonSerializer.IsReflectionEnabledByDefault", false.ToString());

        using var remoteHandle = RemoteExecutor.Invoke(static () =>
        {
            // Arrange
            var options = JsonOptions.DefaultSerializerOptions;

            // Assert
            Assert.NotNull(options.TypeInfoResolver);
            Assert.IsAssignableFrom<IJsonTypeInfoResolver>(options.TypeInfoResolver);
        }, options);
    }

    [ConditionalFact]
    [RemoteExecutionSupported]
    public void DefaultSerializerOptions_SetsTypeInfoResolverToDefault_WhenJsonIsReflectionEnabledByDefaultTrue()
    {
        var options = new RemoteInvokeOptions();
        options.RuntimeConfigurationOptions.Add("System.Text.Json.JsonSerializer.IsReflectionEnabledByDefault", true.ToString());

        using var remoteHandle = RemoteExecutor.Invoke(static () =>
        {
            // Arrange
            var options = JsonOptions.DefaultSerializerOptions;

            // Assert
            Assert.NotNull(options.TypeInfoResolver);
            Assert.IsType<DefaultJsonTypeInfoResolver>(options.TypeInfoResolver);
        }, options);
    }
}

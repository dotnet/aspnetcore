// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Testing;
using Microsoft.DotNet.RemoteExecutor;

namespace Microsoft.AspNetCore.Http.Extensions;

public class JsonOptionsTests
{
    [ConditionalFact]
    [RemoteExecutionSupported]
    public void DefaultSerializerOptions_SetsTypeInfoResolverNull_WhenEnsureJsonTrimmabilityTrue()
    {
        var options = new RemoteInvokeOptions();
        options.RuntimeConfigurationOptions.Add("Microsoft.AspNetCore.EnsureJsonTrimmability", true.ToString());

        using var remoteHandle = RemoteExecutor.Invoke(static () =>
        {
            // Arrange
            var options = JsonOptions.DefaultSerializerOptions;

            // Assert
            Assert.Null(options.TypeInfoResolver);
        }, options);
    }

    [ConditionalFact]
    [RemoteExecutionSupported]
    public void DefaultSerializerOptions_SetsTypeInfoResolverToDefault_WhenEnsureJsonTrimmabilityFalse()
    {
        var options = new RemoteInvokeOptions();
        options.RuntimeConfigurationOptions.Add("Microsoft.AspNetCore.EnsureJsonTrimmability", false.ToString());

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

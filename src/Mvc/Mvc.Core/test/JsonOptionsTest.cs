// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Testing;
using Microsoft.DotNet.RemoteExecutor;

namespace Microsoft.AspNetCore.Mvc;

public partial class JsonOptionsTest
{
    [ConditionalFact]
    [RemoteExecutionSupported]
    public void Configure_ThrowsForNullTypeInfoResolver_WhenEnsureJsonTrimmabilityTrue()
    {
        var options = new RemoteInvokeOptions();
        options.RuntimeConfigurationOptions.Add("Microsoft.AspNetCore.EnsureJsonTrimmability", true.ToString());

        using var remoteHandle = RemoteExecutor.Invoke(static () =>
        {
            // Arrange
            var options = new JsonSerializerOptions();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => new JsonOptions().EnsureConfigured());
        }, options);
    }

    [ConditionalFact]
    [RemoteExecutionSupported]
    public void Configure_Works_WhenEnsureJsonTrimmabilityTrue()
    {
        var options = new RemoteInvokeOptions();
        options.RuntimeConfigurationOptions.Add("Microsoft.AspNetCore.EnsureJsonTrimmability", true.ToString());

        using var remoteHandle = RemoteExecutor.Invoke(static () =>
        {
            // Arrange
            var options = new JsonOptions();
            options.JsonSerializerOptions.AddContext<JsonSerializerExtensionsTestsContext>();

            // Act
            var jsonOptions = options.EnsureConfigured();

            // Assert
            var serializerOptions = options.JsonSerializerOptions;
            Assert.NotNull(serializerOptions.TypeInfoResolver);
            Assert.IsType<JsonSerializerExtensionsTestsContext>(serializerOptions.TypeInfoResolver);
            Assert.True(serializerOptions.IsReadOnly);
        }, options);
    }

    [ConditionalFact]
    [RemoteExecutionSupported]
    public void DefaultSerializerOptions_Works_WhenEnsureJsonTrimmabilityFalse()
    {
        var options = new RemoteInvokeOptions();
        options.RuntimeConfigurationOptions.Add("Microsoft.AspNetCore.EnsureJsonTrimmability", false.ToString());

        using var remoteHandle = RemoteExecutor.Invoke(static () =>
        {
            // Act
            var options = new JsonOptions().EnsureConfigured();

            // Assert
            var serializerOptions = options.JsonSerializerOptions;
            Assert.NotNull(serializerOptions.TypeInfoResolver);
            Assert.IsType<DefaultJsonTypeInfoResolver>(serializerOptions.TypeInfoResolver);
            Assert.True(serializerOptions.IsReadOnly);
        }, options);
    }

    [ConditionalFact]
    [RemoteExecutionSupported]
    public void DefaultSerializerOptions_Combines_WhenEnsureJsonTrimmabilityFalse()
    {
        var options = new RemoteInvokeOptions();
        options.RuntimeConfigurationOptions.Add("Microsoft.AspNetCore.EnsureJsonTrimmability", false.ToString());

        using var remoteHandle = RemoteExecutor.Invoke(static () =>
        {
            // Arrange
            var options = new JsonOptions();
            options.JsonSerializerOptions.AddContext<JsonSerializerExtensionsTestsContext>();

            // Act
            var jsonOptions = options.EnsureConfigured();

            // Assert
            var serializerOptions = options.JsonSerializerOptions;
            Assert.NotNull(serializerOptions.TypeInfoResolver);
            Assert.IsNotType<DefaultJsonTypeInfoResolver>(serializerOptions.TypeInfoResolver);
            Assert.IsNotType<JsonSerializerExtensionsTestsContext>(serializerOptions.TypeInfoResolver);
            Assert.NotNull(serializerOptions.TypeInfoResolver.GetTypeInfo(typeof(string), serializerOptions));
            Assert.True(serializerOptions.IsReadOnly);
        }, options);
    }

    [JsonSerializable(typeof(object))]
    private partial class JsonSerializerExtensionsTestsContext : JsonSerializerContext
    { }
}

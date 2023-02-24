// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Testing;
using Microsoft.DotNet.RemoteExecutor;

namespace Microsoft.AspNetCore.Http.Extensions.Tests;

public partial class JsonSerializerOptionsExtensionsTests
{
    [ConditionalFact]
    [RemoteExecutionSupported]
    public void EnsureConfigured_ThrowsForNullTypeInfoResolver_WhenEnsureJsonTrimmabilityTrue_AndMarkReadonly()
    {
        var options = new RemoteInvokeOptions();
        options.RuntimeConfigurationOptions.Add("Microsoft.AspNetCore.EnsureJsonTrimmability", true.ToString());

        using var remoteHandle = RemoteExecutor.Invoke(static () =>
        {
            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => new JsonSerializerOptions().EnsureConfigured(markAsReadOnly: true));
        }, options);
    }

    [Fact]
    public void EnsureConfigured_MarkAsReadOnly_WhenRequested()
    {
        // Arrange
        var options = new JsonSerializerOptions();

        // Act
        options.EnsureConfigured(markAsReadOnly: true);

        // Assert
        Assert.True(options.IsReadOnly);
    }

    [ConditionalFact]
    [RemoteExecutionSupported]
    public void EnsureConfigured_Works_WhenEnsureJsonTrimmabilityTrue()
    {
        var options = new RemoteInvokeOptions();
        options.RuntimeConfigurationOptions.Add("Microsoft.AspNetCore.EnsureJsonTrimmability", true.ToString());

        using var remoteHandle = RemoteExecutor.Invoke(static () =>
        {
            // Arrange
            var options = new JsonSerializerOptions();
            options.AddContext<JsonSerializerExtensionsTestsContext>();

            // Act
            options.EnsureConfigured();

            // Assert
            Assert.NotNull(options.TypeInfoResolver);
            Assert.IsType<JsonSerializerExtensionsTestsContext>(options.TypeInfoResolver);
            Assert.False(options.IsReadOnly);
        }, options);
    }

    [ConditionalFact]
    [RemoteExecutionSupported]
    public void EnsureConfigured_Works_WhenNullTypeInfoResolverAndEnsureJsonTrimmabilityFalse()
    {
        var options = new RemoteInvokeOptions();
        options.RuntimeConfigurationOptions.Add("Microsoft.AspNetCore.EnsureJsonTrimmability", false.ToString());

        using var remoteHandle = RemoteExecutor.Invoke(static () =>
        {
            // Act
            var options = new JsonSerializerOptions();
            options.EnsureConfigured();

            // Assert
            Assert.NotNull(options.TypeInfoResolver);
            Assert.IsType<DefaultJsonTypeInfoResolver>(options.TypeInfoResolver);
            Assert.False(options.IsReadOnly);
        }, options);
    }

    [ConditionalFact]
    [RemoteExecutionSupported]
    public void EnsureConfigured_DoesNotCombine_WhenResolverAlreadySetAndEnsureJsonTrimmabilityFalse()
    {
        var options = new RemoteInvokeOptions();
        options.RuntimeConfigurationOptions.Add("Microsoft.AspNetCore.EnsureJsonTrimmability", false.ToString());

        using var remoteHandle = RemoteExecutor.Invoke(static () =>
        {
            // Arrange
            var options = new JsonSerializerOptions();
            options.AddContext<JsonSerializerExtensionsTestsContext>();

            // Act
            options.EnsureConfigured();

            // Assert
            Assert.NotNull(options.TypeInfoResolver);
            Assert.IsType<JsonSerializerExtensionsTestsContext>(options.TypeInfoResolver);
            Assert.NotNull(options.TypeInfoResolver.GetTypeInfo(typeof(object), options));
            Assert.False(options.IsReadOnly);
        }, options);
    }

    [JsonSerializable(typeof(object))]
    private partial class JsonSerializerExtensionsTestsContext : JsonSerializerContext
    { }
}

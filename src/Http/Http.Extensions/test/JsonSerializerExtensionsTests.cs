// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Testing;
using Microsoft.DotNet.RemoteExecutor;

namespace Microsoft.AspNetCore.Http.Extensions.Tests;

public partial class JsonSerializerExtensionsTests
{
    //[ConditionalFact]
    //[RemoteExecutionSupported]
    //public void Configure_ThrowsForNullTypeInfoResolver_WhenEnsureJsonTrimmabilityTrue()
    //{
    //    var options = new RemoteInvokeOptions();
    //    options.RuntimeConfigurationOptions.Add("Microsoft.AspNetCore.EnsureJsonTrimmability", true.ToString());

    //    using var remoteHandle = RemoteExecutor.Invoke(static () =>
    //    {
    //        // Arrange
    //        var options = new JsonSerializerOptions();

    //        // Act & Assert
    //        Assert.Throws<InvalidOperationException>(() => JsonSerializerExtensions.EnsureConfigured(options));
    //    }, options);
    //}

    //[ConditionalFact]
    //[RemoteExecutionSupported]
    //public void Configure_Works_WhenEnsureJsonTrimmabilityTrue()
    //{
    //    var options = new RemoteInvokeOptions();
    //    options.RuntimeConfigurationOptions.Add("Microsoft.AspNetCore.EnsureJsonTrimmability", true.ToString());

    //    using var remoteHandle = RemoteExecutor.Invoke(static () =>
    //    {
    //        // Arrange
    //        var options = new JsonSerializerOptions() { TypeInfoResolver = JsonSerializerExtensionsTestsContext.Default };

    //        // Act
    //        JsonSerializerExtensions.EnsureConfigured(options);

    //        // Assert
    //        Assert.NotNull(options.TypeInfoResolver);
    //        Assert.IsType<JsonSerializerExtensionsTestsContext>(options.TypeInfoResolver);
    //        Assert.True(options.IsReadOnly);
    //    }, options);
    //}

    //[ConditionalFact]
    //[RemoteExecutionSupported]
    //public void DefaultSerializerOptions_Works_WhenEnsureJsonTrimmabilityFalse()
    //{
    //    var options = new RemoteInvokeOptions();
    //    options.RuntimeConfigurationOptions.Add("Microsoft.AspNetCore.EnsureJsonTrimmability", false.ToString());

    //    using var remoteHandle = RemoteExecutor.Invoke(static () =>
    //    {
    //        // Arrange
    //        var options = new JsonSerializerOptions();

    //        // Act
    //        JsonSerializerExtensions.EnsureConfigured(options);

    //        // Assert
    //        Assert.NotNull(options.TypeInfoResolver);
    //        Assert.IsType<DefaultJsonTypeInfoResolver>(options.TypeInfoResolver);
    //        Assert.True(options.IsReadOnly);
    //    }, options);
    //}

    //[ConditionalFact]
    //[RemoteExecutionSupported]
    //public void DefaultSerializerOptions_Combines_WhenEnsureJsonTrimmabilityFalse()
    //{
    //    var options = new RemoteInvokeOptions();
    //    options.RuntimeConfigurationOptions.Add("Microsoft.AspNetCore.EnsureJsonTrimmability", false.ToString());

    //    using var remoteHandle = RemoteExecutor.Invoke(static () =>
    //    {
    //        // Arrange
    //        var options = new JsonSerializerOptions() { TypeInfoResolver = JsonSerializerExtensionsTestsContext.Default };

    //        // Act
    //        JsonSerializerExtensions.EnsureConfigured(options);

    //        // Assert
    //        Assert.NotNull(options.TypeInfoResolver);
    //        Assert.IsNotType<DefaultJsonTypeInfoResolver>(options.TypeInfoResolver);
    //        Assert.IsNotType<JsonSerializerExtensionsTestsContext>(options.TypeInfoResolver);
    //        Assert.NotNull(options.TypeInfoResolver.GetTypeInfo(typeof(string), options));
    //        Assert.True(options.IsReadOnly);
    //    }, options);
    //}

    [JsonSerializable(typeof(object))]
    private partial class JsonSerializerExtensionsTestsContext : JsonSerializerContext
    { }
}

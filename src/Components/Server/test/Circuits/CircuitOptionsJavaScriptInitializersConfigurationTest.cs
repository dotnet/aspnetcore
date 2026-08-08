// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.Extensions.FileProviders;
using Moq;

namespace Microsoft.AspNetCore.Components.Server.Circuits;

public class CircuitOptionsJavaScriptInitializersConfigurationTest
{
    [Fact]
    public void MissingManifestLeavesInitializersUnchanged()
    {
        var configuration = CreateConfiguration(content: null, exists: false);
        var options = new CircuitOptions();
        options.JavaScriptInitializers.Add("existing");

        configuration.Configure(options);

        Assert.Equal(["existing"], options.JavaScriptInitializers);
    }

    [Fact]
    public void NullManifestLeavesInitializersUnchanged()
    {
        var configuration = CreateConfiguration("null", exists: true);
        var options = new CircuitOptions();
        options.JavaScriptInitializers.Add("existing");

        configuration.Configure(options);

        Assert.Equal(["existing"], options.JavaScriptInitializers);
    }

    [ConditionalFact]
    [RemoteExecutionSupported]
    public void ReflectionDisabledManifestAppendsInitializersInFileOrder()
    {
        var remoteOptions = new RemoteInvokeOptions();
        remoteOptions.RuntimeConfigurationOptions.Add(
            "System.Text.Json.JsonSerializer.IsReflectionEnabledByDefault",
            false.ToString());

        using var remoteHandle = RemoteExecutor.Invoke(static () =>
        {
            Assert.False(JsonSerializer.IsReflectionEnabledByDefault);
            var configuration = CreateConfiguration("""["first","second"]""", exists: true);
            var options = new CircuitOptions();
            options.JavaScriptInitializers.Add("existing");

            configuration.Configure(options);

            Assert.Equal(["existing", "first", "second"], options.JavaScriptInitializers);
        }, remoteOptions);
    }

    [Fact]
    public void MalformedManifestPreservesJsonException()
    {
        var configuration = CreateConfiguration("""["unterminated]""", exists: true);
        var options = new CircuitOptions();

        Assert.Throws<JsonException>(() => configuration.Configure(options));
    }

    private static CircuitOptionsJavaScriptInitializersConfiguration CreateConfiguration(
        string? content,
        bool exists)
    {
        var fileInfo = new Mock<IFileInfo>();
        fileInfo.SetupGet(file => file.Exists).Returns(exists);
        if (content is not null)
        {
            fileInfo
                .Setup(file => file.CreateReadStream())
                .Returns(() => new MemoryStream(Encoding.UTF8.GetBytes(content)));
        }

        var fileProvider = new Mock<IFileProvider>();
        fileProvider
            .Setup(provider => provider.GetFileInfo("TestApp.modules.json"))
            .Returns(fileInfo.Object);

        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(value => value.ApplicationName).Returns("TestApp");
        environment.SetupGet(value => value.WebRootFileProvider).Returns(fileProvider.Object);
        return new CircuitOptionsJavaScriptInitializersConfiguration(environment.Object);
    }
}

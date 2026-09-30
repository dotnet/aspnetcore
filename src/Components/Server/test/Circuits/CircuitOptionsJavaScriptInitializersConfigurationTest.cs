// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Moq;

namespace Microsoft.AspNetCore.Components.Server.Tests.Circuits;

public class CircuitOptionsJavaScriptInitializersConfigurationTest
{
    [Fact]
    public void Configure_ReadsJavaScriptInitializersFromManifest()
    {
        var file = new Mock<IFileInfo>();
        file.SetupGet(f => f.Exists).Returns(true);
        file.Setup(f => f.CreateReadStream()).Returns(
            () => new MemoryStream("""["./first.js","./second.js"]"""u8.ToArray()));

        var fileProvider = new Mock<IFileProvider>();
        fileProvider.Setup(p => p.GetFileInfo("TestApp.modules.json")).Returns(file.Object);

        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(e => e.ApplicationName).Returns("TestApp");
        environment.SetupGet(e => e.WebRootFileProvider).Returns(fileProvider.Object);

        var options = new CircuitOptions();
        var configuration = new CircuitOptionsJavaScriptInitializersConfiguration(environment.Object);

        configuration.Configure(options);

        Assert.Equal(["./first.js", "./second.js"], options.JavaScriptInitializers);
    }
}

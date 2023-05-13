// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Xml.Linq;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Microsoft.AspNetCore.DataProtection.KeyManagement;

public class KeyEscrowServiceProviderExtensionsTests
{
    [Fact]
    public void GetKeyEscrowSink_NullServiceProvider_ReturnsNull()
    {
        Assert.Null(((IServiceProvider)null).GetKeyEscrowSink());
    }

    [Fact]
    public void GetKeyEscrowSink_EmptyServiceProvider_ReturnsNull()
    {
        // Arrange
        var services = new ServiceCollection().BuildServiceProvider();

        // Act & assert
        Assert.Null(services.GetKeyEscrowSink());
    }

    [Fact]
    public void GetKeyEscrowSink_SingleKeyEscrowRegistration_ReturnsAggregateOverSingleSink()
    {
        // Arrange
        List<string> output = new List<string>();

        var mockKeyEscrowSink = new Mock<IKeyEscrowSink>();
        mockKeyEscrowSink.Setup(o => o.Store(It.IsAny<Guid>(), It.IsAny<XElement>()))
            .Callback<Guid, XElement>((keyId, element) =>
            {
                output.Add(string.Format(CultureInfo.InvariantCulture, "{0:D}: {1}", keyId, element.Name.LocalName));
            });

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<IKeyEscrowSink>(mockKeyEscrowSink.Object);
        var services = serviceCollection.BuildServiceProvider();

        // Act
        var sink = services.GetKeyEscrowSink();
        sink.Store(new Guid("39974d8e-3e53-4d78-b7e9-4ff64a2a5d7b"), XElement.Parse("<theElement />"));

        // Assert
        Assert.Equal(new[] { "39974d8e-3e53-4d78-b7e9-4ff64a2a5d7b: theElement" }, output);
    }

    [Fact]
    public void GetKeyEscrowSink_MultipleKeyEscrowRegistration_ReturnsAggregate()
    {
        // Arrange
        List<string> output = new List<string>();

        var mockKeyEscrowSink1 = new Mock<IKeyEscrowSink>();
        mockKeyEscrowSink1.Setup(o => o.Store(It.IsAny<Guid>(), It.IsAny<XElement>()))
            .Callback<Guid, XElement>((keyId, element) =>
            {
                output.Add(string.Format(CultureInfo.InvariantCulture, "[sink1] {0:D}: {1}", keyId, element.Name.LocalName));
            });

        var mockKeyEscrowSink2 = new Mock<IKeyEscrowSink>();
        mockKeyEscrowSink2.Setup(o => o.Store(It.IsAny<Guid>(), It.IsAny<XElement>()))
            .Callback<Guid, XElement>((keyId, element) =>
            {
                output.Add(string.Format(CultureInfo.InvariantCulture, "[sink2] {0:D}: {1}", keyId, element.Name.LocalName));
            });

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<IKeyEscrowSink>(mockKeyEscrowSink1.Object);
        serviceCollection.AddSingleton<IKeyEscrowSink>(mockKeyEscrowSink2.Object);
        var services = serviceCollection.BuildServiceProvider();

        // Act
        var sink = services.GetKeyEscrowSink();
        sink.Store(new Guid("39974d8e-3e53-4d78-b7e9-4ff64a2a5d7b"), XElement.Parse("<theElement />"));

        // Assert
        Assert.Equal(new[] { "[sink1] 39974d8e-3e53-4d78-b7e9-4ff64a2a5d7b: theElement", "[sink2] 39974d8e-3e53-4d78-b7e9-4ff64a2a5d7b: theElement" }, output);
    }
}

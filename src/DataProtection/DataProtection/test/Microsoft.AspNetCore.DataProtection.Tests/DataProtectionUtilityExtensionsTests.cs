// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.DataProtection.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.InternalTesting;
using Moq;

namespace Microsoft.AspNetCore.DataProtection;

public class DataProtectionUtilityExtensionsTests
{
    [ConditionalTheory]
    [InlineData("app-path", "app-path\\")]
    [InlineData("app-path ", "app-path\\")] // normalized trim
    [InlineData("app-path\\", "app-path\\")]
    [InlineData("app-path \\", "app-path \\")]
    [InlineData("app-path/", "app-path/")]
    [InlineData("app-path /", "app-path /")]
    [InlineData(" /", "/")]
    [InlineData(" \\ ", "\\")]
    [InlineData("  ", null)] // normalized whitespace -> null
    [InlineData(null, null)] // nothing provided at all
    [OSSkipCondition(OperatingSystems.Linux)]
    [OSSkipCondition(OperatingSystems.MacOSX)]
    public void GetApplicationUniqueIdentifierFromHostingWindows(string contentRootPath, string expected)
    {
        // Arrange
        var mockEnvironment = new Mock<IHostEnvironment>();
        mockEnvironment.Setup(o => o.ContentRootPath).Returns(contentRootPath);

        var services = new ServiceCollection()
            .AddSingleton(mockEnvironment.Object)
            .AddDataProtection()
            .Services
            .BuildServiceProvider();

        // Act
        var actual = services.GetApplicationUniqueIdentifier();

        // Assert
        Assert.Equal(expected, actual);
    }

    [ConditionalTheory]
    [InlineData("app-path", "app-path/")]
    [InlineData("app-path ", "app-path/")] // normalized trim
    [InlineData("app-path\\", "app-path\\/")]
    [InlineData("app-path \\", "app-path \\/")]
    [InlineData("app-path/", "app-path/")]
    [InlineData("app-path /", "app-path /")]
    [InlineData(" /", "/")]
    [InlineData(" \\ ", "\\/")]
    [InlineData("  ", null)] // normalized whitespace -> null
    [InlineData(null, null)] // nothing provided at all
    [OSSkipCondition(OperatingSystems.Windows)]
    public void GetApplicationUniqueIdentifierFromHostingNonWindows(string contentRootPath, string expected)
    {
        // Arrange
        var mockEnvironment = new Mock<IHostEnvironment>();
        mockEnvironment.Setup(o => o.ContentRootPath).Returns(contentRootPath);

        var services = new ServiceCollection()
            .AddSingleton(mockEnvironment.Object)
            .AddDataProtection()
            .Services
            .BuildServiceProvider();

        // Act
        var actual = services.GetApplicationUniqueIdentifier();

        // Assert
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(" discriminator ", "discriminator")]
    [InlineData(" discriminator", "discriminator")] // normalized trim
    [InlineData("  ", null)] // normalized whitespace -> null
    [InlineData(null, null)] // nothing provided at all
    public void GetApplicationIdentifierFromApplicationDiscriminator(string discriminator, string expected)
    {
        // Arrange
        var mockAppDiscriminator = new Mock<IApplicationDiscriminator>();
        mockAppDiscriminator.Setup(o => o.Discriminator).Returns(discriminator);

        var mockEnvironment = new Mock<IHostEnvironment>();
        mockEnvironment.SetupGet(o => o.ContentRootPath).Throws(new InvalidOperationException("Hosting environment should not be checked"));

        var services = new ServiceCollection()
            .AddSingleton(mockEnvironment.Object)
            .AddSingleton(mockAppDiscriminator.Object)
            .AddDataProtection()
            .Services
            .BuildServiceProvider();

        // Act
        var actual = services.GetApplicationUniqueIdentifier();

        // Assert
        Assert.Equal(expected, actual);
        mockAppDiscriminator.VerifyAll();
    }

    [Fact]
    public void GetApplicationUniqueIdentifier_NoServiceProvider_ReturnsNull()
    {
        Assert.Null(((IServiceProvider)null).GetApplicationUniqueIdentifier());
    }

    [Fact]
    public void GetApplicationUniqueIdentifier_NoHostingEnvironment_ReturnsNull()
    {
        // arrange
        var services = new ServiceCollection()
          .AddDataProtection()
          .Services
          .BuildServiceProvider();

        // act & assert
        Assert.Null(services.GetApplicationUniqueIdentifier());
    }
}

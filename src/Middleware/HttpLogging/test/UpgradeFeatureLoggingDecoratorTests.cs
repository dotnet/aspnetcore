// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Features;
using Moq;

namespace Microsoft.AspNetCore.HttpLogging.Tests;
public class UpgradeFeatureLoggingDecoratorTests
{
    private readonly Mock<IHttpUpgradeFeature> upgradeFeatureMock;
    private readonly Mock<Action> loggingDelegateMock;

    public UpgradeFeatureLoggingDecoratorTests()
    {
        upgradeFeatureMock = new();
        loggingDelegateMock = new();
    }

    [Fact]
    public void Ctor_ThrowsExceptionsWhenNullArgs()
    {
        Assert.Throws<ArgumentNullException>(() => new UpgradeFeatureLoggingDecorator(
            null,
            loggingDelegateMock.Object));

        Assert.Throws<ArgumentNullException>(() => new UpgradeFeatureLoggingDecorator(
            upgradeFeatureMock.Object,
            null));
    }

    [Fact]
    public void IsUpgradableRequest_ShouldForwardToWrappedFeature()
    {
        var decorator = new UpgradeFeatureLoggingDecorator(upgradeFeatureMock.Object, loggingDelegateMock.Object);

        var isUpgradableRequest = decorator.IsUpgradableRequest;

        Assert.Null(
            Record.Exception(() => upgradeFeatureMock.Verify(m => m.IsUpgradableRequest, Times.Once)));
        Assert.Null(
            Record.Exception(() => upgradeFeatureMock.VerifyNoOtherCalls()));
    }

    [Fact]
    public async Task UpgradeAsync_ShouldForwardToWrappedFeatureAndThenLog()
    {
        var decorator = new UpgradeFeatureLoggingDecorator(upgradeFeatureMock.Object, loggingDelegateMock.Object);

        await decorator.UpgradeAsync();

        Assert.Null(
            Record.Exception(() => upgradeFeatureMock.Verify(m => m.UpgradeAsync(), Times.Once)));
        Assert.Null(
            Record.Exception(() => upgradeFeatureMock.VerifyNoOtherCalls()));
        Assert.Null(
            Record.Exception(() => loggingDelegateMock.Verify(m => m(), Times.Once)));
        Assert.Null(
            Record.Exception(() => loggingDelegateMock.VerifyNoOtherCalls()));
    }
}

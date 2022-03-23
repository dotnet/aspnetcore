// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Features;
using Moq;

namespace Microsoft.AspNetCore.HttpLogging.Tests;
public class UpgradeFeatureLoggingDecoratorTests
{
    private readonly Mock<IHttpUpgradeFeature> _upgradeFeatureMock;
    private readonly Mock<Action> _loggingDelegateMock;

    public UpgradeFeatureLoggingDecoratorTests()
    {
        _upgradeFeatureMock = new();
        _loggingDelegateMock = new();
    }

    [Fact]
    public void Ctor_ThrowsExceptionsWhenNullArgs()
    {
        Assert.Throws<ArgumentNullException>(() => new UpgradeFeatureLoggingDecorator(
            null,
            _loggingDelegateMock.Object));

        Assert.Throws<ArgumentNullException>(() => new UpgradeFeatureLoggingDecorator(
            _upgradeFeatureMock.Object,
            null));
    }

    [Fact]
    public void IsUpgradableRequest_ShouldForwardToWrappedFeature()
    {
        var decorator = new UpgradeFeatureLoggingDecorator(_upgradeFeatureMock.Object, _loggingDelegateMock.Object);

        var isUpgradableRequest = decorator.IsUpgradableRequest;

        Assert.Null(
            Record.Exception(() => _upgradeFeatureMock.Verify(m => m.IsUpgradableRequest, Times.Once)));
        Assert.Null(
            Record.Exception(() => _upgradeFeatureMock.VerifyNoOtherCalls()));
    }

    [Fact]
    public async Task UpgradeAsync_ShouldForwardToWrappedFeatureAndThenLog()
    {
        var decorator = new UpgradeFeatureLoggingDecorator(_upgradeFeatureMock.Object, _loggingDelegateMock.Object);

        await decorator.UpgradeAsync();

        Assert.Null(
            Record.Exception(() => _upgradeFeatureMock.Verify(m => m.UpgradeAsync(), Times.Once)));
        Assert.Null(
            Record.Exception(() => _upgradeFeatureMock.VerifyNoOtherCalls()));
        Assert.Null(
            Record.Exception(() => _loggingDelegateMock.Verify(m => m(), Times.Once)));
        Assert.Null(
            Record.Exception(() => _loggingDelegateMock.VerifyNoOtherCalls()));
    }
}

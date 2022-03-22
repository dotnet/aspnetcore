// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.HttpLogging;

internal sealed class UpgradeFeatureLoggingDecorator : IHttpUpgradeFeature
{
    private readonly IHttpUpgradeFeature _innerUpgradeFeature;
    private readonly Action _loggingDelegate;

    public UpgradeFeatureLoggingDecorator(IHttpUpgradeFeature innerUpgradeFeature, Action loggingDelegate)
    {
        _innerUpgradeFeature = innerUpgradeFeature ?? throw new ArgumentNullException(nameof(innerUpgradeFeature));
        _loggingDelegate = loggingDelegate ?? throw new ArgumentNullException(nameof(loggingDelegate));
    }

    public bool IsUpgradableRequest => _innerUpgradeFeature.IsUpgradableRequest;

    public async Task<Stream> UpgradeAsync()
    {
        var upgradeTask = await _innerUpgradeFeature.UpgradeAsync();

        _loggingDelegate();

        return upgradeTask;
    }
}

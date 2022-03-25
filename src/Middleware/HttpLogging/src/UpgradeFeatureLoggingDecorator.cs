// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.HttpLogging;

internal sealed class UpgradeFeatureLoggingDecorator : IHttpUpgradeFeature
{
    private readonly IHttpUpgradeFeature _innerUpgradeFeature;
    private readonly Action _loggingDelegate;
    private bool _isUpgraded;

    public UpgradeFeatureLoggingDecorator(IHttpUpgradeFeature innerUpgradeFeature, Action loggingDelegate)
    {
        _innerUpgradeFeature = innerUpgradeFeature ?? throw new ArgumentNullException(nameof(innerUpgradeFeature));
        _loggingDelegate = loggingDelegate ?? throw new ArgumentNullException(nameof(loggingDelegate));
    }

    public bool IsUpgradableRequest => _innerUpgradeFeature.IsUpgradableRequest;

    public bool IsUpgraded { get => _isUpgraded; }

    public async Task<Stream> UpgradeAsync()
    {
        var upgradeStream = await _innerUpgradeFeature.UpgradeAsync();

        _isUpgraded = true;

        _loggingDelegate();

        return upgradeStream;
    }
}

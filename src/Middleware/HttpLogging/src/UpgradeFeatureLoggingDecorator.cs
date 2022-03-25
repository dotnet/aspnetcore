// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.HttpLogging;

internal sealed class UpgradeFeatureLoggingDecorator : IHttpUpgradeFeature
{
    private readonly IHttpUpgradeFeature _innerUpgradeFeature;
    private readonly HttpResponse _response;
    private readonly HttpLoggingOptions _options;
    private readonly ILogger _logger;

    private bool _isUpgraded;

    public UpgradeFeatureLoggingDecorator(IHttpUpgradeFeature innerUpgradeFeature, HttpResponse response, HttpLoggingOptions options, ILogger logger)
    {
        _innerUpgradeFeature = innerUpgradeFeature ?? throw new ArgumentNullException(nameof(innerUpgradeFeature));
        _response = response ?? throw new ArgumentNullException(nameof(response));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public bool IsUpgradableRequest => _innerUpgradeFeature.IsUpgradableRequest;

    public bool IsUpgraded { get => _isUpgraded; }

    public async Task<Stream> UpgradeAsync()
    {
        var upgradeStream = await _innerUpgradeFeature.UpgradeAsync();

        _isUpgraded = true;

        HttpLoggingMiddleware.LogResponseHeaders(_response, _options, _logger);

        return upgradeStream;
    }
}

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
    private readonly HashSet<string> _allowedResponseHeaders;
    private readonly ILogger _logger;
    private readonly HttpLoggingFields _loggingFields;

    private bool _isUpgraded;

    public UpgradeFeatureLoggingDecorator(IHttpUpgradeFeature innerUpgradeFeature, HttpResponse response, HashSet<string> allowedResponseHeaders, HttpLoggingFields loggingFields, ILogger logger)
    {
        _innerUpgradeFeature = innerUpgradeFeature ?? throw new ArgumentNullException(nameof(innerUpgradeFeature));
        _response = response ?? throw new ArgumentNullException(nameof(response));
        _allowedResponseHeaders = allowedResponseHeaders ?? throw new ArgumentNullException(nameof(allowedResponseHeaders));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _loggingFields = loggingFields;
    }

    public bool IsUpgradableRequest => _innerUpgradeFeature.IsUpgradableRequest;

    public bool IsUpgraded { get => _isUpgraded; }

    public async Task<Stream> UpgradeAsync()
    {
        var upgradeStream = await _innerUpgradeFeature.UpgradeAsync();

        _isUpgraded = true;

        HttpLoggingMiddleware.LogResponseHeaders(_response, _loggingFields, _allowedResponseHeaders, _logger);

        return upgradeStream;
    }
}

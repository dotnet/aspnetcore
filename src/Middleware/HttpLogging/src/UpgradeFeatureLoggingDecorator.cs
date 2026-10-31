// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.HttpLogging;

internal sealed class UpgradeFeatureLoggingDecorator : IHttpUpgradeFeature
{
    private readonly IHttpUpgradeFeature _innerUpgradeFeature;
    private readonly HttpLoggingInterceptorContext _logContext;
    private readonly HttpLoggingOptions _options;
    private readonly IHttpLoggingInterceptor[] _interceptors;
    private readonly ILogger _logger;

    private bool _isUpgraded;

    public UpgradeFeatureLoggingDecorator(IHttpUpgradeFeature innerUpgradeFeature, HttpLoggingInterceptorContext logContext, HttpLoggingOptions options,
        IHttpLoggingInterceptor[] interceptors, ILogger logger)
    {
        _innerUpgradeFeature = innerUpgradeFeature ?? throw new ArgumentNullException(nameof(innerUpgradeFeature));
        _logContext = logContext ?? throw new ArgumentNullException(nameof(logContext));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _interceptors = interceptors;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public bool IsUpgradableRequest => _innerUpgradeFeature.IsUpgradableRequest;

    public bool IsUpgraded { get => _isUpgraded; }

    public async Task<Stream> UpgradeAsync()
    {
        var upgradeStream = await _innerUpgradeFeature.UpgradeAsync();
        _isUpgraded = true;

        await HttpLoggingMiddleware.LogResponseHeadersAsync(_logContext, _options, _interceptors, _logger);

        return upgradeStream;
    }
}

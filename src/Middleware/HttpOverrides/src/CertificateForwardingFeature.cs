// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.HttpOverrides;

internal sealed class CertificateForwardingFeature : ITlsConnectionFeature
{
    private readonly ILogger _logger;
    private readonly StringValues _header;
    private readonly CertificateForwardingOptions _options;
    private Task<X509Certificate2?>? _certificateTask;

    public CertificateForwardingFeature(ILogger logger, StringValues header, CertificateForwardingOptions options)
    {
        _logger = logger;
        _options = options;
        _header = header;
    }

    public X509Certificate2? ClientCertificate
    {
        get => GetClientCertificateAsync(CancellationToken.None).Result;
        set => _certificateTask = value is not null ? Task.FromResult<X509Certificate2?>(value) : null;
    }

    public Task<X509Certificate2?> GetClientCertificateAsync(CancellationToken cancellationToken)
    {
        if (_certificateTask == null)
        {
            try
            {
                var certificate = _options.HeaderConverter(_header.ToString());
                _certificateTask = Task.FromResult<X509Certificate2?>(certificate);
                return _certificateTask;
            }
            catch (Exception e)
            {
                _logger.NoCertificate(e);
                return Task.FromResult<X509Certificate2?>(null);
            }
        }
        else
        {
            return _certificateTask;
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Server.IISIntegration;

internal sealed class ForwardedTlsConnectionFeature : ITlsConnectionFeature
{
    private StringValues _header;
    private X509Certificate2? _certificate;
    private readonly ILogger _logger;

    public ForwardedTlsConnectionFeature(ILogger logger, StringValues header)
    {
        _logger = logger;
        _header = header;
    }

    public X509Certificate2? ClientCertificate
    {
        get
        {
            if (_certificate == null && _header != StringValues.Empty)
            {
                try
                {
                    var bytes = Convert.FromBase64String(_header.ToString());
                    _certificate = new X509Certificate2(bytes);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(0, ex, "Failed to read the client certificate.");
                }
            }
            return _certificate;
        }
        set
        {
            _certificate = value;
            _header = StringValues.Empty;
        }
    }

    public Task<X509Certificate2?> GetClientCertificateAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(ClientCertificate);
    }
}

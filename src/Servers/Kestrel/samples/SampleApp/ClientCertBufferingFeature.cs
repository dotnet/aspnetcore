// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.WebUtilities;

namespace SampleApp;

internal static class ClientCertBufferingExtensions
{
    // Buffers HTTP/1.x request bodies received over TLS (https) if a client certificate needs to be negotiated.
    // This avoids the issue where POST data is received during the certificate negotiation:
    // InvalidOperationException: Received data during renegotiation.
    public static IApplicationBuilder UseClientCertBuffering(this IApplicationBuilder builder)
    {
        return builder.Use((context, next) =>
        {
            var tlsFeature = context.Features.Get<ITlsConnectionFeature>();
            var bodyFeature = context.Features.Get<IHttpRequestBodyDetectionFeature>();
            var connectionItems = context.Features.Get<IConnectionItemsFeature>();

            // Look for TLS connections that don't already have a client cert, and requests that could have a body.
            if (tlsFeature != null && tlsFeature.ClientCertificate == null && bodyFeature.CanHaveBody
            && !connectionItems.Items.TryGetValue("tls.clientcert.negotiated", out var _))
            {
                context.Features.Set<ITlsConnectionFeature>(new ClientCertBufferingFeature(tlsFeature, context));
            }

            return next(context);
        });
    }
}

internal class ClientCertBufferingFeature : ITlsConnectionFeature
{
    private readonly ITlsConnectionFeature _tlsFeature;
    private readonly HttpContext _context;

    public ClientCertBufferingFeature(ITlsConnectionFeature tlsFeature, HttpContext context)
    {
        _tlsFeature = tlsFeature;
        _context = context;
    }

    public X509Certificate2 ClientCertificate
    {
        get => _tlsFeature.ClientCertificate;
        set => _tlsFeature.ClientCertificate = value;
    }

    public async Task<X509Certificate2> GetClientCertificateAsync(CancellationToken cancellationToken)
    {
        // Note: This doesn't set its own size limit for the buffering or draining, it relies on the server's
        // 30mb default request size limit.
        if (!_context.Request.Body.CanSeek)
        {
            _context.Request.EnableBuffering();
        }

        var body = _context.Request.Body;
        await body.DrainAsync(cancellationToken);
        body.Position = 0;

        // Negative caching, prevent buffering on future requests even if the client does not give a cert when prompted.
        var connectionItems = _context.Features.Get<IConnectionItemsFeature>();
        connectionItems.Items["tls.clientcert.negotiated"] = true;

        return await _tlsFeature.GetClientCertificateAsync(cancellationToken);
    }
}

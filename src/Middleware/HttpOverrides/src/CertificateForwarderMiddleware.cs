// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.HttpOverrides
{
    /// <summary>
    /// Middleware that converts a forward header into a client certificate if found.
    /// </summary>
    public class CertificateForwarderMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly CertificateForwarderOptions _options;
        private readonly ILogger _logger;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="next"></param>
        /// <param name="loggerFactory"></param>
        /// <param name="options"></param>
        public CertificateForwarderMiddleware(
                RequestDelegate next,
                ILoggerFactory loggerFactory,
                IOptions<CertificateForwarderOptions> options)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));

            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _options = options.Value;
            _logger = loggerFactory.CreateLogger<CertificateForwarderMiddleware>();
        }

        /// <summary>
        /// Looks for the presence of a <see cref="CertificateForwarderOptions.CertificateHeader"/> header in the request,
        /// if found, converts this header to a ClientCertificate set on the connection.
        /// </summary>
        /// <param name="httpContext">The <see cref="HttpContext"/>.</param>
        /// <returns>A <see cref="Task"/>.</returns>
        public async Task Invoke(HttpContext httpContext)
        {
            if (!string.IsNullOrEmpty(_options.CertificateHeader))
            {
                var clientCertificate = await httpContext.Connection.GetClientCertificateAsync();

                if (clientCertificate == null)
                {
                    // Check for forwarding header
                    string certificateHeader = httpContext.Request.Headers[_options.CertificateHeader];
                    if (!string.IsNullOrEmpty(certificateHeader))
                    {
                        try
                        {
                            httpContext.Connection.ClientCertificate = _options.HeaderConverter(certificateHeader);
                        }
                        catch
                        {
                            _logger.LogError("Could not read certificate from header.");
                        }
                    }
                }
            }

            await _next(httpContext);
        }
    }
}

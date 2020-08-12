// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Logging.AzureAppServices
{
    internal class BlobLoggerConfigureOptions : BatchLoggerConfigureOptions, IConfigureOptions<AzureBlobLoggerOptions>
    {
        private readonly IConfiguration _configuration;
        private readonly IWebAppContext _context;

        public BlobLoggerConfigureOptions(IConfiguration configuration, IWebAppContext context)
            : base(configuration, "AzureBlobEnabled")
        {
            _configuration = configuration;
            _context = context;
        }

        public void Configure(AzureBlobLoggerOptions options)
        {
            base.Configure(options);
            options.ContainerUrl = _configuration.GetSection("APPSETTING_DIAGNOSTICS_AZUREBLOBCONTAINERSASURL")?.Value;
            options.ApplicationName = _context.SiteName;
            options.ApplicationInstanceId = _context.SiteInstanceId;
        }
    }
}

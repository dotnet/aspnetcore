// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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

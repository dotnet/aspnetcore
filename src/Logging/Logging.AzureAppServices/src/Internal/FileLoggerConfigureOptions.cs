// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Logging.AzureAppServices.Internal
{
    public class FileLoggerConfigureOptions : BatchLoggerConfigureOptions, IConfigureOptions<AzureFileLoggerOptions>
    {
        private readonly IWebAppContext _context;

        public FileLoggerConfigureOptions(IConfiguration configuration, IWebAppContext context)
            : base(configuration, "AzureDriveEnabled")
        {
            _context = context;
        }

        public void Configure(AzureFileLoggerOptions options)
        {
            base.Configure(options);
            options.LogDirectory = Path.Combine(_context.HomeFolder, "LogFiles", "Application");
        }
    }
}
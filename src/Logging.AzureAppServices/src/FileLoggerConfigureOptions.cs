// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Logging.AzureAppServices
{
    internal class FileLoggerConfigureOptions : BatchLoggerConfigureOptions, IConfigureOptions<AzureFileLoggerOptions>
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

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Logging.AzureAppServices
{
    internal class BatchLoggerConfigureOptions : IConfigureOptions<BatchingLoggerOptions>
    {
        private readonly IConfiguration _configuration;
        private readonly string _isEnabledKey;

        public BatchLoggerConfigureOptions(IConfiguration configuration, string isEnabledKey)
        {
            _configuration = configuration;
            _isEnabledKey = isEnabledKey;
        }

        public void Configure(BatchingLoggerOptions options)
        {
            options.IsEnabled = TextToBoolean(_configuration.GetSection(_isEnabledKey)?.Value);
        }

        private static bool TextToBoolean(string text)
        {
            if (string.IsNullOrEmpty(text) ||
                !bool.TryParse(text, out var result))
            {
                result = false;
            }

            return result;
        }
    }
}

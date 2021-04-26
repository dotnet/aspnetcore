// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Extensions.Logging.AzureAppServices
{
    public readonly struct AzureBlobLoggerContext
    {
        public AzureBlobLoggerContext(string appName, string identifier, DateTimeOffset timestamp)
        {
            AppName = appName;
            Identifier = identifier;
            Timestamp = timestamp;
        }

        public string AppName { get; }
        public string Identifier { get; }
        public DateTimeOffset Timestamp { get; }
    }
}

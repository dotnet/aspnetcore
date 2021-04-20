using System;

namespace Microsoft.Extensions.Logging.AzureAppServices
{
    public readonly struct AzureBlobLoggerContext
    {
        public AzureBlobLoggerContext(string appName, string identifier, DateTimeOfset timestamp);
        public string AppName { get; }
        public string Identifier { get; }
        public DateTimeOffset Timestamp { get; }
    }
}

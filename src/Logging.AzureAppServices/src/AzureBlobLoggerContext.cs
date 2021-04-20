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

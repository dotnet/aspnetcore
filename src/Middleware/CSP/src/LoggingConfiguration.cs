using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Csp
{
    public class LoggingConfiguration
    {
        public LogLevel LogLevel { get; set; }

        public string ReportUri { get; set; }
    }
}

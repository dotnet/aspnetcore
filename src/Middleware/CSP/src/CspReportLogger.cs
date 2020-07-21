using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Csp
{
    public interface ICspReportLoggerFactory
    {
        CspReportLogger BuildLogger(LogLevel logLevel, string reportUri);
    }

    public class CspReportLoggerFactory : ICspReportLoggerFactory
    {
        private readonly ILogger<CspReportLogger> _logger;
        public CspReportLoggerFactory(ILogger<CspReportLogger> logger)
        {
            _logger = logger;
        }

        public CspReportLogger BuildLogger(LogLevel logLevel, string reportUri)
        {
            return new CspReportLogger
            {
                LogLevel = logLevel,
                ReportUri = reportUri,
                Logger = _logger
            };
        }
    }

    public class CspReportLogger
    {
        public virtual void Log(CspReport report)
        {
            Logger.Log(LogLevel, TextualizeReport(report));
        }

        private string TextualizeReport(CspReport report)
        {
            if (LogLevel.CompareTo(LogLevel.Information) > 0)
            {
                //switch (report.ReportData.ViolatedDirective)
                //{

                //}
            } else
            {

            }
            return report.ToString();
        }

        public LogLevel LogLevel { get; internal set; }
        public string ReportUri { get; internal set; }
        public ILogger<CspReportLogger> Logger { get; internal set; }
    }
}

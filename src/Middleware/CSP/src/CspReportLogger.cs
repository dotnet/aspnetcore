using System.IO;
using System.Text.Json;
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
        private readonly JsonSerializerOptions _serializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        public virtual async void Log(Stream jsonReport)
        {
            if (LogLevel.CompareTo(LogLevel.Information) >= 0)
            {
                try
                {
                    CspReport cspReport = await JsonSerializer.DeserializeAsync<CspReport>(jsonReport, _serializerOptions);
                    if (cspReport.ReportData != null)
                    {
                        Logger.Log(LogLevel, TextualizeReport(cspReport));
                    }
                }
                catch (JsonException)
                {
                }

                return;
            }

            Logger.Log(LogLevel, new StreamReader(jsonReport).ReadToEnd());
        }

        private string TextualizeReport(CspReport report)
        {
            if (LogLevel.CompareTo(LogLevel.Information) >= 0)
            {
                if (CspConstants.BlockedUriInline.Equals(report.ReportData.BlockedUri))
                {
                    // javascript: URI not allowed
                    if (CspConstants.ScriptSrcElem.Equals(report.ReportData.ViolatedDirective))
                    {
                        return string.Format("Attempt to navigate to javascript URI from {0} was refused by policy", report.ReportData.DocumentUri);
                    }
                    // inline event handler
                    else if (CspConstants.ScriptSrcAttr.Equals(report.ReportData.ViolatedDirective))
                    {
                        return string.Format("Inline event handler at {0} (line number {1}) was refused by policy", report.ReportData.DocumentUri, report.ReportData.LineNumber);
                    }
                }
                // script is missing or doesn't match nonce in the policy
                else if (CspConstants.ScriptSrcElem.Equals(report.ReportData.ViolatedDirective))
                {
                    return string.Format(
                        "Script at {0} (line {1}) trying to load {2} refused to run due to missing or mismatching nonce value",
                        report.ReportData.DocumentUri,
                        report.ReportData.LineNumber,
                        report.ReportData.BlockedUri
                    );
                }
            }

            return report.ToString();
        }

        public LogLevel LogLevel { get; internal set; }
        public string ReportUri { get; internal set; }
        public ILogger<CspReportLogger> Logger { get; internal set; }
    }
}

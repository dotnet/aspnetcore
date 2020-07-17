using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Csp
{
    public class CspReportingMiddleware
    {
        private readonly LoggingConfiguration _loggingConfig;
        private readonly ILogger<CspReportingMiddleware> _logger;
        private readonly JsonSerializerOptions _serializerOptions;

        public CspReportingMiddleware(RequestDelegate next, LoggingConfiguration loggingConfiguration, ILogger<CspReportingMiddleware> logger)
        {
            _loggingConfig = loggingConfiguration;
            _logger = logger;
            _serializerOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };
        }

        private bool IsReportRequest(HttpRequest request)
        {
            // TODO: Is this first condition guaranteed?
            return request.Path.StartsWithSegments(_loggingConfig.ReportUri)
                && request.ContentType?.StartsWith(CspConstants.CspReportContentType) == true
                && request.ContentLength != 0;
        }

        private async void HandleIncomingReport(Stream body)
        {
            try
            {
                CspReport cspReport = await JsonSerializer.DeserializeAsync<CspReport>(body, _serializerOptions);
                if (cspReport.ReportData != null)
                {
                    _logger.Log(_loggingConfig.LogLevel, TextualizeReport(cspReport, _loggingConfig.LogLevel));
                }
            } catch (JsonException)
            {
                return;
            }
        }

        // TODO: Implement ToString on reportData
        private string TextualizeReport(CspReport cspReport, LogLevel logLevel)
        {
            return cspReport.ReportData.ToString();
        }

        public Task Invoke(HttpContext context)
        {
            if (IsReportRequest(context.Request))
            {
                HandleIncomingReport(context.Request.Body);
            }

            context.Response.StatusCode = (int) HttpStatusCode.NoContent;
            // TODO: Is there a better way to write an empty response?
            return context.Response.WriteAsync("");
        }
    }
}

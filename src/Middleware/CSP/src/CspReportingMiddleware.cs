using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Csp
{
    public class CspReportingMiddleware
    {
        private readonly CspReportLogger _cspReportLogger;

        public CspReportingMiddleware(RequestDelegate next, CspReportLogger reportLogger)
        {
            _cspReportLogger = reportLogger;
        }

        private bool IsReportRequest(HttpRequest request)
        {
            return request.Path.StartsWithSegments(_cspReportLogger.ReportUri)
                && request.Method == HttpMethods.Post
                && request.ContentType?.StartsWith(CspConstants.CspReportContentType) == true
                && request.ContentLength != 0;
        }

        public Task Invoke(HttpContext context)
        {
            if (IsReportRequest(context.Request))
            {
                _cspReportLogger.Log(context.Request.Body);
            }

            context.Response.StatusCode = (int)HttpStatusCode.NoContent;
            return Task.FromResult(0);
        }
    }
}

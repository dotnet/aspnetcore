using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Core.Infrastructure;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BasicWebSite.Filters
{
    public class RedirectAntiforgeryValidationFailedResultFilter : IAlwaysRunResultFilter
    {
        public void OnResultExecuting(ResultExecutingContext context)
        {
            if (context.Result is IAntiforgeryValidationFailedResult result)
            {
                context.Result = new RedirectResult("http://example.com/antiforgery-redirect");
            }
        }

        public void OnResultExecuted(ResultExecutedContext context)
        { }
    }
}

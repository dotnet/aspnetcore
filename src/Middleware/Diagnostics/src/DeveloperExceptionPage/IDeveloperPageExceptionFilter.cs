using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Diagnostics
{
    public interface IDeveloperPageExceptionFilter
    {
        Task HandleExceptionAsync(HttpContext context, Exception exception, Func<HttpContext, Exception, Task> next);
    }
}

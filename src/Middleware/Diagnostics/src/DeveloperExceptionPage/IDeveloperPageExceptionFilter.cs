using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Diagnostics
{
    public interface IDeveloperPageExceptionFilter
    {
        Task HandleExceptionAsync(ErrorContext errorContext, Func<ErrorContext, Task> next);
    }
}

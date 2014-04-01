using System;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc
{
    public interface IAsyncExceptionFilter : IFilter
    {
        Task OnActionExecutedAsync([NotNull] ExceptionContext context);
    }
}

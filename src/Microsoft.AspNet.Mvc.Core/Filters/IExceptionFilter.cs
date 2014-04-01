using System;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc
{
    public interface IExceptionFilter : IFilter
    {
        void OnActionExecuted([NotNull] ExceptionContext context);
    }
}

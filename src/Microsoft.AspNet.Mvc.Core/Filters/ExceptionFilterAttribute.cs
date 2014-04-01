using System;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public abstract class ExceptionFilterAttribute : Attribute, IAsyncExceptionFilter, IExceptionFilter, IOrderedFilter
    {
        public int Order { get; set; }

        #pragma warning disable 1998
        public async Task OnActionExecutedAsync([NotNull] ExceptionContext context)
        {
            OnActionExecuted(context);
        }
        #pragma warning restore 1998

        public void OnActionExecuted([NotNull] ExceptionContext context)
        {
        }
    }
}

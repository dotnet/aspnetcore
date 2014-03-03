using System;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public abstract class ExceptionFilterAttribute : Attribute, IExceptionFilter, IFilter
    {
        public abstract Task Invoke(ExceptionFilterContext context, Func<ExceptionFilterContext, Task> next);

        public int Order { get; set; }
    }
}

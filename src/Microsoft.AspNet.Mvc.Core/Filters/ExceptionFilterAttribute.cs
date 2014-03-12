using System;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public abstract class ExceptionFilterAttribute : Attribute, IExceptionFilter, IOrderedFilter
    {
        public abstract Task Invoke(ExceptionFilterContext context, Func<Task> next);

        public int Order { get; set; }
    }
}

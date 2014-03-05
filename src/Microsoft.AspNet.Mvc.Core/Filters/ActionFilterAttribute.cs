using System;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public abstract class ActionFilterAttribute : Attribute, IActionFilter, IFilter
    {
        public abstract Task Invoke(ActionFilterContext context, Func<Task> next);

        public int Order { get; set; }
    }
}

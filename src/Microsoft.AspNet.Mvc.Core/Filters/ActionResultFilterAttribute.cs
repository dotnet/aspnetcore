using System;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public abstract class ActionResultFilterAttribute : Attribute, IActionResultFilter, IFilter
    {
        public abstract Task Invoke(ActionResultFilterContext context, Func<Task> next);

        public int Order { get; set; }
    }
}

using System;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc
{
    public abstract class ActionResult : IActionResult
    {
        public virtual Task ExecuteResultAsync(ActionContext context)
        {
            ExecuteResult(context);
            return Task.FromResult(true);
        }

        public virtual void ExecuteResult(ActionContext context)
        {
        }
    }
}
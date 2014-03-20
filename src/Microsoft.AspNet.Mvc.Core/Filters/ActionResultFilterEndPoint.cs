using System;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc.Filters
{
    // This one lives in the Filters namespace, and only intended to be consumed by folks that rewrite the action invoker.
    public class ActionResultFilterEndPoint : IActionResultFilter
    {
        public async Task Invoke(ActionResultFilterContext context, Func<Task> next)
        {
            // result can get cleared at any point in the pipeline
            if (context.ActionResult == null)
            {
                context.ActionResult = new EmptyResult();
            }

            await context.ActionResult.ExecuteResultAsync(context.ActionContext);
        }
    }
}

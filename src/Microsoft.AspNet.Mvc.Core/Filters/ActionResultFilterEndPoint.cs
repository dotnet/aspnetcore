using System;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc.Filters
{
    public class ActionResultFilterEndPoint : IActionResultFilter
    {
        public async Task Invoke(ActionResultFilterContext context, Func<Task> next)
        {
            await context.Result.ExecuteResultAsync(context.ActionContext);
        }
    }
}

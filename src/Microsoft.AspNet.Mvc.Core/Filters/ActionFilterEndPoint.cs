using System;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc.Filters
{
    public class ReflectedActionFilterEndPoint : IActionFilter
    {
        private readonly Func<object[], Task<object>> _coreMethodInvoker;
        private readonly IActionResultFactory _actionResultFactory;

        public ReflectedActionFilterEndPoint(Func<object[], Task<object>> coreMethodInvoker,
                                             IActionResultFactory actionResultFactory)
        {
            _coreMethodInvoker = coreMethodInvoker;
            _actionResultFactory = actionResultFactory;
        }

        public async Task Invoke(ActionFilterContext context, Func<Task> next)
        {
            // TODO: match the parameter names here.
            var tempArray = context.ActionParameters.Values.ToArray(); // seriously broken for now, need to organize names to match.

            var actionReturnValue = await _coreMethodInvoker(tempArray);

            context.Result = _actionResultFactory.CreateActionResult(context.MethodReturnType, 
                                                                     actionReturnValue,
                                                                     context.ActionContext);
        }
    }
}

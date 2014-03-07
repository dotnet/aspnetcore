using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.Core;

namespace Microsoft.AspNet.Mvc.Filters
{
    // This one lives in the Filters namespace, and only intended to be consumed by folks that rewrite the action invoker.
    public class ReflectedActionFilterEndPoint : IActionFilter
    {
        private readonly IActionResultFactory _actionResultFactory;
        private readonly object _controllerInstance;

        public ReflectedActionFilterEndPoint(IActionResultFactory actionResultFactory, object controllerInstance)
        {
            _actionResultFactory = actionResultFactory;
            _controllerInstance = controllerInstance;
        }

        public async Task Invoke(ActionFilterContext context, Func<Task> next)
        {
            var reflectedActionDescriptor = context.ActionContext.ActionDescriptor as ReflectedActionDescriptor;
            if (reflectedActionDescriptor == null)
            {
                throw new ArgumentException(Resources.ReflectedActionFilterEndPoint_UnexpectedActionDescriptor);
            }

            var actionMethodInfo = reflectedActionDescriptor.MethodInfo;
            var actionReturnValue = await ReflectedActionExecutor.ExecuteAsync(
                                                            actionMethodInfo,
                                                            _controllerInstance,
                                                            context.ActionArguments);

            var underlyingReturnType = TypeHelper.GetTaskInnerTypeOrNull(actionMethodInfo.ReturnType) ?? actionMethodInfo.ReturnType;
            context.Result = _actionResultFactory.CreateActionResult(
                                                            underlyingReturnType,
                                                            actionReturnValue,
                                                            context.ActionContext);
        }
    }
}
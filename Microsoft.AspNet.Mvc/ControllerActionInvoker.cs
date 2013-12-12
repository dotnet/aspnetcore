using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc
{
    public class ControllerActionInvoker : IActionInvoker
    {
        private readonly ControllerContext _context;
        private readonly IActionResultFactory _actionResultFactory;

        public ControllerActionInvoker(ControllerContext context, IActionResultFactory actionResultFactory)
        {
            _context = context;
            _actionResultFactory = actionResultFactory;
        }

        public Task InvokeActionAsync(string actionName)
        {
            var method = _context.Controller.GetType().GetMethod(actionName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

            if (method == null)
            {
                throw new InvalidOperationException(String.Format("Could not find action method '{0}'", actionName));
            }

            object actionReturnValue = method.Invoke(_context.Controller, null);

            IActionResult actionResult = _actionResultFactory.CreateActionResult(actionReturnValue);

            return actionResult.ExecuteResultAsync(_context);
        }
    }
}

using System;
using System.Globalization;
using System.Reflection;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc
{
    public class ControllerActionInvoker : IActionInvoker
    {
        private ControllerContext _context;

        public ControllerActionInvoker(ControllerContext context)
        {
            _context = context;
        }

        public Task InvokeActionAsync(string actionName)
        {
            var method = _context.Controller.GetType().GetMethod(actionName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

            if (method == null)
            {
                throw new InvalidOperationException(String.Format("Could not find action method '{0}'", actionName));
            }

            object actionReturnValue = method.Invoke(_context.Controller, null); ;

            IActionResult actionResult = CreateResult(actionReturnValue);

            return actionResult.ExecuteResultAsync(_context);
        }

        private IActionResult CreateResult(object actionReturnValue)
        {
            IActionResult actionResult = actionReturnValue as IActionResult;

            if (actionResult != null)
            {
                return actionResult;
            }

            if (actionReturnValue != null)
            {
                return new ContentResult
                {
                    Content = Convert.ToString(actionReturnValue, CultureInfo.InvariantCulture)
                };
            }

            return new EmptyResult();
        }
    }
}

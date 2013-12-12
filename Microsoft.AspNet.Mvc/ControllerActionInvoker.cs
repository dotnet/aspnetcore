using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            var method = _context.Controller.GetType().GetMethod(actionName);

            if (method == null)
            {
                throw new InvalidOperationException(String.Format("Could not find action method '{0}'", actionName));
            }

            method.Invoke(_context.Controller, null);

            return Task.FromResult(0);
        }
    }
}

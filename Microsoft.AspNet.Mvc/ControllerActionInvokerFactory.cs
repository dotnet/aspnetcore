using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.AspNet.Mvc
{
    public class ControllerActionInvokerFactory : IActionInvokerFactory
    {
        public IActionInvoker CreateInvoker(ControllerContext context)
        {
            return new ControllerActionInvoker(context);
        }
    }
}

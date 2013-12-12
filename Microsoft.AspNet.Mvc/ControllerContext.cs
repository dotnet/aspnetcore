using System;
using Microsoft.Owin;

namespace Microsoft.AspNet.Mvc
{
    public class ControllerContext
    {
        public ControllerContext(IOwinContext context, object controller)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            if (controller == null)
            {
                throw new ArgumentNullException("controller");
            }

            HttpContext = context;
            Controller = controller;
        }

        public virtual object Controller { get; set; }

        public virtual IOwinContext HttpContext { get; set; }
    }
}

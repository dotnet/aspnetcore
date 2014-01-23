using System;

namespace Microsoft.AspNet.Mvc
{
    public class ActionResultFactory : IActionResultFactory
    {
        public IActionResult CreateActionResult(Type declaredReturnType, object actionReturnValue, RequestContext requestContext)
        {
            return new ContentResult
            {
                ContentType = "text/plain",
                Content = Convert.ToString(actionReturnValue),
            };
        }
    }
}

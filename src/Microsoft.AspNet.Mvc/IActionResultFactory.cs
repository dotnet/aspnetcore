using System;

namespace Microsoft.AspNet.Mvc
{
    public interface IActionResultFactory
    {
        IActionResult CreateActionResult(Type declaredReturnType, object actionReturnValue, RequestContext requestContext);
    }
}

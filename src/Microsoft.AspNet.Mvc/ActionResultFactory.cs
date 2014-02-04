using System;

namespace Microsoft.AspNet.Mvc
{
    public class ActionResultFactory : IActionResultFactory
    {
        public IActionResult CreateActionResult(Type declaredReturnType, object actionReturnValue, RequestContext requestContext)
        {
            // optimize common path
            IActionResult actionResult = actionReturnValue as IActionResult;

            if (actionResult != null)
            {
                return actionResult;
            }

            if (declaredReturnType == null)
            {
                throw new InvalidOperationException("Declared type must be passed");
            }

            if (typeof(IActionResult).IsAssignableFrom(declaredReturnType) && actionReturnValue == null)
            {
                throw new InvalidOperationException("Cannot return null from an action method declaring IActionResult or HttpResponseMessage");
            }

            if (declaredReturnType.IsGenericParameter)
            {
                // This can happen if somebody declares an action method as:
                // public T Get<T>() { }
                throw new InvalidOperationException("HttpActionDescriptor_NoConverterForGenericParamterTypeExists");
            }

            if (declaredReturnType.IsAssignableFrom(typeof(void)))
            {
                return new NoContentResult();
            }

            if (actionReturnValue is string)
            {
                return new ContentResult
                {
                    ContentType = "text/plain",
                    Content = (string)actionReturnValue,
                };
            }

            return new JsonResult(actionReturnValue);
        }
    }
}

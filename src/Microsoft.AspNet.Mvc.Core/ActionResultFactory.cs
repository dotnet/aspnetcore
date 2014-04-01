using System;

namespace Microsoft.AspNet.Mvc
{
    public class ActionResultFactory : IActionResultFactory
    {
        private readonly IActionResultHelper _result;

        public ActionResultFactory(IActionResultHelper result)
        {
            _result = result;
        }

        public IActionResult CreateActionResult(Type declaredReturnType, object actionReturnValue, ActionContext actionContext)
        {
            // optimize common path
            var actionResult = actionReturnValue as IActionResult;

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

            if (declaredReturnType == typeof(void) || actionReturnValue == null)
            {
                return new NoContentResult();
            }

            var actionReturnString = actionReturnValue as string;

            if (actionReturnString != null)
            {
                return new ContentResult
                {
                    ContentType = "text/plain",
                    Content = actionReturnString,
                };
            }

            return _result.Json(actionReturnValue);
        }
    }
}

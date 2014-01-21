using Microsoft.AspNet.CoreServices;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Linq;

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

            bool isDeclaredTypeActionResult = typeof(IActionResult).IsAssignableFrom(declaredReturnType);
            bool isDeclaredTypeResponseMessage = typeof(HttpResponseMessage).IsAssignableFrom(declaredReturnType);

            if ((isDeclaredTypeActionResult || isDeclaredTypeResponseMessage) && actionReturnValue == null)
            {
                throw new InvalidOperationException("Cannot return null from an action method declaring IActionResult or HttpResponseMessage");
            }

            if (declaredReturnType == null)
            {
                throw new InvalidOperationException("Declared type must be passed");
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

            var responseMessage = actionReturnValue as HttpResponseMessage;
            if (responseMessage != null)
            {
                return new HttpResponseMessageActionResult(responseMessage);
            }

            if (actionReturnValue is string)
            {
                return new ContentResult
                {
                    ContentType = "text/plain",
                    Content = (string)actionReturnValue,
                };
            }

            // TODO: this needs to get injected
            IOwinContentNegotiator contentNegotiator = new DefaultContentNegotiator();

            // TODO: inject the formatters
            IEnumerable<MediaTypeFormatter> formatters = requestContext.Formatters;

            return new NegotiatedContentResult(HttpStatusCode.OK, declaredReturnType, actionReturnValue, contentNegotiator, requestContext.HttpContext, formatters);
        }
    }
}

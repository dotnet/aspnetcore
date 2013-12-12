using System.Net.Http;
using System.Net.Http.Formatting;

namespace Microsoft.AspNet.Mvc
{
    public class ActionResultFactory : IActionResultFactory
    {
        public IActionResult CreateActionResult(object actionReturnValue)
        {
            var actionResult = actionReturnValue as IActionResult;

            if (actionResult != null)
            {
                return actionResult;
            }

            var responseMessage = actionReturnValue as HttpResponseMessage;
            if (responseMessage != null)
            {
                return new HttpResponseMessageActionResult(responseMessage);
            }

            // all other object types are treated as an http response message action result
            var content = new ObjectContent(actionReturnValue.GetType(),
                                            actionReturnValue,
                                            new JsonMediaTypeFormatter());

            return new HttpResponseMessageActionResult(new HttpResponseMessage
            {
                Content = content
            });
        }
    }
}

using Microsoft.AspNet.Mvc;

namespace MvcSample.Web.Filters
{
    public class UserNameProvider : ActionFilterAttribute
    {
        private static readonly string[] _userNames = new[] { "Jon", "David", "Goliath" };
        private static int _index;

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            object originalUserName = null;

            context.ActionArguments.TryGetValue("userName", out originalUserName);

            var userName = originalUserName as string;

            if (string.IsNullOrWhiteSpace(userName))
            {
                context.ActionArguments["userName"] = _userNames[(_index++)%3];
            }
        }
    }
}

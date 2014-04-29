using Microsoft.AspNet.Mvc;

namespace MvcSample.Web.Filters
{
    public class UserNameProvider : IActionFilter
    {
        private readonly UserNameService _nameService;

        public UserNameProvider(UserNameService nameService)
        {
            _nameService = nameService;
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            object originalUserName = null;

            context.ActionArguments.TryGetValue("userName", out originalUserName);

            var userName = originalUserName as string;

            if (string.IsNullOrWhiteSpace(userName))
            {
                context.ActionArguments["userName"] = _nameService.GetName();
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
        }
    }
}

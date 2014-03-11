using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;

namespace MvcSample.Filters
{
    public class UserNameProvider : ActionFilterAttribute
    {
        private static readonly string[] _userNames = new[] { "Jon", "David", "Goliath" };
        private static int _index;

        public override async Task Invoke(ActionFilterContext context, Func<Task> next)
        {
            object originalUserName = null;

            context.ActionParameters.TryGetValue("userName", out originalUserName);

            var userName = originalUserName as string;

            if (string.IsNullOrWhiteSpace(userName))
            {
                context.ActionParameters["userName"] = _userNames[(_index++)%3];
            }

            await next();
        }
    }
}

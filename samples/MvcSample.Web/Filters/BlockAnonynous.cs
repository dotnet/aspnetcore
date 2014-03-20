using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;

namespace MvcSample.Web.Filters
{
    public class BlockAnonynous : AuthorizationFilterAttribute
    {
        public override async Task Invoke(AuthorizationFilterContext context, Func<Task> next)
        {
            if (!context.HasAllowAnonymous())
            {
                context.Fail();
            }

            await next();
        }
    }
}
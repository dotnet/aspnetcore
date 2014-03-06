using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;

namespace MvcSample
{
    public class PassThroughAttribute : AuthorizationFilterAttribute
    {
        public async override Task Invoke(AuthorizationFilterContext context, Func<Task> next)
        {
            await next();
        }
    }
}

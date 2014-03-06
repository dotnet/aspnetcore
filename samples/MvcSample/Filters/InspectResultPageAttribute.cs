using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using MvcSample.Models;

namespace MvcSample.Filters
{
    public class InspectResultPageAttribute : ActionResultFilterAttribute
    {
        public async override Task Invoke(ActionResultFilterContext context, Func<Task> next)
        {
            ViewResult viewResult = context.Result as ViewResult;

            if (viewResult != null)
            {
                User user = viewResult.ViewData.Model as User;

                if (user != null)
                {
                    user.Name += "**" + user.Name + "**";
                }
            }

            await next();
        }
    }
}

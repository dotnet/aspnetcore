using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using MvcSample.Web.Models;

namespace MvcSample.Web.Filters
{
    public class InspectResultPageAttribute : ActionResultFilterAttribute
    {
        public async override Task Invoke(ActionResultFilterContext context, Func<Task> next)
        {
            var viewResult = context.ActionResult as ViewResult;

            if (viewResult != null)
            {
                var user = viewResult.ViewData.Model as User;

                if (user != null)
                {
                    user.Name += "**" + user.Name + "**";
                }
            }

            await next();
        }
    }
}

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Filters;
using MvcSample.Web.Models;

namespace MvcSample.Web.Filters
{
    public class InspectResultPageAttribute : ActionFilterAttribute
    {
        public override async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
        {
            var viewResult = context.Result as ViewResult;

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

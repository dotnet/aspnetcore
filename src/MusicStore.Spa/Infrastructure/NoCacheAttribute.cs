using System;
using Microsoft.AspNet.Mvc;

namespace MusicStore.Spa.Infrastructure
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class NoCacheAttribute : ActionFilterAttribute
    {
        public override void OnResultExecuting(ResultExecutingContext context)
        {
            context.HttpContext.Response.Headers["Cache-Control"] = "private, max-age=0";

            base.OnResultExecuting(context);
        }
    }
}
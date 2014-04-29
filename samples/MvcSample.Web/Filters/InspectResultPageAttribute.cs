using System;
using Microsoft.AspNet.Mvc;
using MvcSample.Web.Models;

namespace MvcSample.Web.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class InspectResultPageAttribute : Attribute, IFilterFactory
    {
        public IFilter CreateInstance(IServiceProvider serviceProvider)
        {
            return new InspectResultPageFilter();
        }

        private class InspectResultPageFilter : IResultFilter
        {
            public void OnResultExecuting(ResultExecutingContext context)
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
            }

            public void OnResultExecuted(ResultExecutedContext context)
            {
            }
        }
    }
}

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;

namespace MvcSample.Web.Filters
{
    public class AgeEnhancerAttribute : ActionFilterAttribute
    {
        public async override Task Invoke(ActionFilterContext context, Func<Task> next)
        {
            object age = null;

            if (context.ActionParameters.TryGetValue("age", out age))
            {
                if (age is int)
                {
                    var intAge = (int) age;

                    if (intAge < 21)
                    {
                        intAge += 5;
                    }
                    else if (intAge > 30)
                    {
                        intAge = 29;
                    }

                    context.ActionParameters["age"] = intAge;
                }
            }

            await next();
        }
    }
}

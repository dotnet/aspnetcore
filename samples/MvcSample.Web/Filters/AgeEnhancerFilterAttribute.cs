using System;
using Microsoft.AspNet.Mvc;

namespace MvcSample.Web.Filters
{
    public class AgeEnhancerAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            object age = null;

            var controller = context.Controller as FiltersController;

            if (controller != null)
            {
                controller.User.Log += "Age Enhanced!" + Environment.NewLine;
            }
            
            if (context.ActionArguments.TryGetValue("age", out age))
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

                    context.ActionArguments["age"] = intAge;
                }
            }
        }
    }
}

using Microsoft.AspNet.Mvc;

namespace MvcSample.Web.Filters
{
    public class AgeEnhancerAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            object age = null;

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

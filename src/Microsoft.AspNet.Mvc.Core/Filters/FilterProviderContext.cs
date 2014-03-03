using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc
{
    public class FilterProviderContext
    {
        public FilterProviderContext(ActionDescriptor actionDescriptor)
        {
            ActionDescriptor = actionDescriptor;
        }

        // Input
        public ActionDescriptor ActionDescriptor { get; set; }

        // Results
        public List<IAuthorizationFilter> AuthorizationFilters { get; set; }

        public List<IActionFilter> ActionFilters { get; set; }

        public List<IActionResultFilter> ActionResultFilters { get; set; }

        public List<IExceptionFilter> ExceptionFilters { get; set; }
    }
}

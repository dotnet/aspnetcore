using Microsoft.AspNet.Mvc.Filters;

namespace Microsoft.AspNet.Mvc
{
    public interface IActionFilter : IFilter<ActionFilterContext>
    {
    }
}

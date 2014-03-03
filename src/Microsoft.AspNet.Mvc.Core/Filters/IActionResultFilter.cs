using Microsoft.AspNet.Mvc.Filters;

namespace Microsoft.AspNet.Mvc
{
    public interface IActionResultFilter : IFilter<ActionResultFilterContext>
    {
    }
}

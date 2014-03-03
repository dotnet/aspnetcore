using Microsoft.AspNet.Mvc.Filters;

namespace Microsoft.AspNet.Mvc
{
    public interface IExceptionFilter : IFilter<ExceptionFilterContext>
    {
    }
}

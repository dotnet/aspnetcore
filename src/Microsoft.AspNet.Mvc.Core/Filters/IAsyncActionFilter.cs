using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc
{
    public interface IAsyncActionFilter : IFilter
    {
        Task OnActionExecutionAsync([NotNull] ActionExecutingContext context, [NotNull] ActionExecutionDelegate next);
    }
}

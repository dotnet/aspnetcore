using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc
{
    public interface IAsyncResultFilter : IFilter
    {
        Task OnResultExecutionAsync([NotNull] ResultExecutingContext context, [NotNull] ResultExecutionDelegate next);
    }
}

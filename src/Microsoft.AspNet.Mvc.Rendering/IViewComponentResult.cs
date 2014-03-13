
using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc
{
    public interface IViewComponentResult
    {
        void Execute([NotNull] ViewComponentContext context);

        Task ExecuteAsync([NotNull] ViewComponentContext context);
    }
}

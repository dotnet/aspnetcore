
using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc
{
    public interface IViewComponentInvoker
    {
        void Invoke([NotNull] ViewComponentContext context);

        Task InvokeAsync([NotNull] ViewComponentContext context);
    }
}

using System;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc
{
    public interface IAsyncAuthorizationFilter : IFilter
    {
        Task OnAuthorizationAsync([NotNull] AuthorizationContext context);
    }
}

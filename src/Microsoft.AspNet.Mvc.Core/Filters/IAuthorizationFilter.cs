using System;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc
{
    public interface IAuthorizationFilter : IFilter
    {
        void OnAuthorization([NotNull] AuthorizationContext context);
    }
}

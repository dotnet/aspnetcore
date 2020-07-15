using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Csp
{
    public interface IContentSecurityPolicyProvider
    {

        Task<ContentSecurityPolicy> GetPolicyAsync(HttpContext context);
    }
}

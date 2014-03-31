using System.Security.Claims;
using System.Security.Principal;

namespace Microsoft.AspNet.Mvc
{
    // Can extract unique identifers for a claims-based identity
    public interface IClaimUidExtractor
    {
        string ExtractClaimUid(ClaimsIdentity identity);
    }
}
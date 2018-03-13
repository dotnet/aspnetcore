using System.Security.Claims;

namespace Microsoft.AspNetCore.Protocols.Features
{
    public interface IConnectionUserFeature
    {
        ClaimsPrincipal User { get; set; }
    }
}
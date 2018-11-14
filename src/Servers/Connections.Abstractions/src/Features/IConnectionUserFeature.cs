using System.Security.Claims;

namespace Microsoft.AspNetCore.Connections.Features
{
    public interface IConnectionUserFeature
    {
        ClaimsPrincipal User { get; set; }
    }
}
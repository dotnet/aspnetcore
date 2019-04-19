
using System.Security.Claims;

namespace Microsoft.AspNetCore.Authentication.Negotiate
{
    internal interface INegotiateState
    {
        string GetOutgoingBlob(string incomingBlob);

        bool IsCompleted { get; }

        ClaimsPrincipal GetPrincipal();
    }
}

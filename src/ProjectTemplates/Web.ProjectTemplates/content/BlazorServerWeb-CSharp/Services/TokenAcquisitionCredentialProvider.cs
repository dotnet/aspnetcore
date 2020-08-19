using Microsoft.Graph;
using Microsoft.Identity.Web;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace BlazorServerWeb_CSharp
{
    internal class TokenAcquisitionCredentialProvider : IAuthenticationProvider
    {
        public TokenAcquisitionCredentialProvider(ITokenAcquisition tokenAcquisition, IEnumerable<string> initialScopes)
        {
            _tokenAcquisition = tokenAcquisition;
            _initialScopes = initialScopes;
        }

        ITokenAcquisition _tokenAcquisition;
        IEnumerable<string> _initialScopes;

        public async Task AuthenticateRequestAsync(HttpRequestMessage request)
        {
            request.Headers.Add("Authorization",
                $"Bearer {await _tokenAcquisition.GetAccessTokenForUserAsync(_initialScopes)}");
        }
    }
}

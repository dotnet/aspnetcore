using System;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Csp
{
    public class CspService : ICspService
    {
        public CspService()
        {

        }

        public void ApplyResult(HttpResponse response)
        {
            throw new NotImplementedException();
        }
    }
}

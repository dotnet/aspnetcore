using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Csp
{
    public interface ICspService
    {
        void ApplyResult(HttpResponse response);
    }
}

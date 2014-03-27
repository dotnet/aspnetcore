using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.HttpFeature.Security;
using Microsoft.AspNet.PipelineCore.Security;

namespace Microsoft.AspNet.Security.Infrastructure
{
    internal static class HttpContextExtensions
    {
        internal static IHttpAuthentication GetAuthentication(this HttpContext context)
        {
            var auth = context.GetFeature<IHttpAuthentication>();
            if (auth == null)
            {
                auth = new DefaultHttpAuthentication();
                context.SetFeature<IHttpAuthentication>(auth);
            }
            return auth;
        }
    }
}

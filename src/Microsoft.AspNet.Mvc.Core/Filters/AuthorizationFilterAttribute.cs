using System;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public abstract class AuthorizationFilterAttribute : Attribute, IFilter, IAuthorizationFilter
    {
        public abstract Task Invoke(AuthorizationFilterContext context, Func<AuthorizationFilterContext, Task> next);

        public int Order { get; set; }
    }
}

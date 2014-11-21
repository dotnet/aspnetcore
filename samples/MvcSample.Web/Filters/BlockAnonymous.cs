using Microsoft.AspNet.Mvc;

namespace MvcSample.Web.Filters
{
    public class BlockAnonymous : AuthorizationFilterAttribute
    {
        public override void OnAuthorization(AuthorizationContext context)
        {
            if (!HasAllowAnonymous(context))
            {
                var user = context.HttpContext.User;
                var userIsAnonymous =
                    user == null ||
                    user.Identity == null ||
                    !user.Identity.IsAuthenticated;

                if (userIsAnonymous)
                {
                    base.Fail(context);
                }
            }
        }
    }
}
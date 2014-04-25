using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc
{
    public class EmptyResult : ActionResult
    {
        private static readonly EmptyResult _singleton = new EmptyResult();

        internal static EmptyResult Instance
        {
            get { return _singleton; }
        }

        public override void ExecuteResult([NotNull] ActionContext context)
        {
            context.HttpContext.Response.StatusCode = 204;
        }
    }
}

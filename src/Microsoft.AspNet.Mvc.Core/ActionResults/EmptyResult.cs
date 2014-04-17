using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc
{
    public class EmptyResult : IActionResult
    {
        private static readonly EmptyResult _singleton = new EmptyResult();

        internal static EmptyResult Instance
        {
            get { return _singleton; }
        }

        #pragma warning disable 1998
        public async Task ExecuteResultAsync(ActionContext context)
        {
            context.HttpContext.Response.StatusCode = 204;
        }
        #pragma warning restore 1998
    }
}

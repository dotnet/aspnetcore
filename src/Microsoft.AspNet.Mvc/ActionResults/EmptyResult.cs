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

        public async Task ExecuteResultAsync(ActionContext context)
        {
        }
    }
}

using System.Threading.Tasks;
using Microsoft.Owin;

namespace Microsoft.AspNet.Mvc
{
    public class Controller
    { 
        public void Initialize(IActionResultHelper actionResultHelper)
        {
            Result = actionResultHelper;
            ViewData = new ViewDataDictionary();
        }

        public IActionResultHelper Result { get; private set; }

        public IOwinContext Context { get; set; }

        public ViewDataDictionary ViewData { get; private set; }
    }
}

using System.Threading.Tasks;
using Microsoft.Owin;

namespace Microsoft.AspNet.Mvc
{
    public class Controller
    { 
        public void Initialize(IActionResultHelper actionResultHelper)
        {
            Result = actionResultHelper;
        }

        public IActionResultHelper Result { get; private set; }

        public IOwinContext Context { get; set; }
    }
}

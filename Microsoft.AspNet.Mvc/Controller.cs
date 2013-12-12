using System.Threading.Tasks;
using Microsoft.Owin;

namespace Microsoft.AspNet.Mvc
{
    public class Controller
    {
        public void Initialize(IOwinContext context)
        {
            Context = context;
        }

        public IOwinContext Context { get; set; }
    }
}

using System.Threading.Tasks;
using Microsoft.Owin;

namespace Microsoft.AspNet.Mvc
{
    public interface IController
    {
        Task Execute(IOwinContext context);
    }
}

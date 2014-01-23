using System.Threading.Tasks;

namespace Microsoft.AspNet.Abstractions
{
    public delegate Task RequestDelegate(HttpContext context);
}
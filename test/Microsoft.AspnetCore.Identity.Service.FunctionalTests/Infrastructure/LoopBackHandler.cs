using System.Net.Http;

namespace Microsoft.AspnetCore.Identity.Service.FunctionalTests
{
    /// <summary>
    /// A handler used to create a loop back into TestServer from the open ID Connect handler.
    /// </summary>
    public class LoopBackHandler : DelegatingHandler
    {
    }
}

using System;

namespace Microsoft.AspNet.Server.KestrelTests
{
    /// <summary>
    /// Summary description for Program
    /// </summary>
    public class Program
    {
        public void Main()
        {
            new EngineTests().DisconnectingClient().Wait();
        }
    }
}
using System;

namespace Microsoft.AspNet.Server.KestralTests
{
    /// <summary>
    /// Summary description for Program
    /// </summary>
    public class Program
    {
        public void Main()
        {
            new EngineTests().Http11().Wait();
        }
    }
}
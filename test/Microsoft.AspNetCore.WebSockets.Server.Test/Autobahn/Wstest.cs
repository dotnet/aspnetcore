using System;

namespace Microsoft.AspNetCore.WebSockets.Server.Test.Autobahn
{
    /// <summary>
    /// Wrapper around the Autobahn Test Suite's "wstest" app.
    /// </summary>
    public class Wstest : Executable
    {
        private static Lazy<Wstest> _instance = new Lazy<Wstest>(Create);

        public static Wstest Default => _instance.Value;

        public Wstest(string path) : base(path) { }

        private static Wstest Create()
        {
            var location = Locate("wstest");
            return location == null ? null : new Wstest(location);
        }
    }
}

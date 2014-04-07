using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class HttpPutAttribute : Attribute, IActionHttpMethodProvider
    {
        private static readonly IEnumerable<string> _supportedMethods = new string[] { "PUT" };

        public IEnumerable<string> HttpMethods
        {
            get { return _supportedMethods; }
        }
    }
}
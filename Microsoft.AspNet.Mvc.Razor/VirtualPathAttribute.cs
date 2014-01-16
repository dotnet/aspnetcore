using System;

namespace Microsoft.AspNet.Mvc.Razor
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class VirtualPathAttribute : Attribute
    {
        public VirtualPathAttribute(string virtualPath)
        {
            VirtualPath = virtualPath;
        }

        public string VirtualPath { get; private set; }
    }
}

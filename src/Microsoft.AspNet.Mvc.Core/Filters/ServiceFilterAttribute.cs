using System;
using System.Diagnostics;

namespace Microsoft.AspNet.Mvc
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    [DebuggerDisplay("ServiceFilter: Type={ServiceType} Order={Order}")]
    public class ServiceFilterAttribute : Attribute, IServiceFilter
    {
        public ServiceFilterAttribute(Type type)
        {
            ServiceType = type;
        }

        public Type ServiceType { get; private set; }

        public int Order { get; set; }
    }
}

using System;
using System.Diagnostics;

namespace Microsoft.AspNet.Mvc
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    [DebuggerDisplay("TypeFilter: Type={ImplementationType} Order={Order}")]
    public class TypeFilterAttribute : Attribute, ITypeFilter, IOrderedFilter
    {
        public TypeFilterAttribute(Type type)
        {
            ImplementationType = type;
        }

        public object[] Arguments { get; set; }

        public Type ImplementationType { get; private set; }

        public int Order { get; set; }
    }
}
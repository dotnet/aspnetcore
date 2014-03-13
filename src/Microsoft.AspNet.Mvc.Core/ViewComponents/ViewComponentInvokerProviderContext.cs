
using System;
using System.Reflection;

namespace Microsoft.AspNet.Mvc
{
    public class ViewComponentInvokerProviderContext
    {
        public ViewComponentInvokerProviderContext([NotNull] TypeInfo componentType, object[] arguments)
        {
            ComponentType = componentType;
            Arguments = arguments;
        }

        public object[] Arguments { get; private set; }

        public TypeInfo ComponentType { get; private set; }

        public IViewComponentInvoker Result { get; set; }
    }
}

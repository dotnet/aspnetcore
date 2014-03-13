
using System.Reflection;

namespace Microsoft.AspNet.Mvc
{
    public interface IViewComponentInvokerFactory
    {
        IViewComponentInvoker CreateInstance([NotNull] TypeInfo componentType, object[] args);
    }
}

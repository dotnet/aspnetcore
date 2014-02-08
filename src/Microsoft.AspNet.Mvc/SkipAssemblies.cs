using System.Reflection;

namespace Microsoft.AspNet.Mvc
{
    public abstract class SkipAssemblies
    {
        public static readonly string ControllerDiscoveryScope = "DCS";

        public abstract bool Skip(Assembly assembly, string scope);
    }
}

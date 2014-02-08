using System.Reflection;

namespace Microsoft.AspNet.Mvc
{
    public class SkipNoAssemblies : SkipAssemblies
    {
        public override bool Skip(Assembly assembly, string scope)
        {
            return false;
        }
    }
}

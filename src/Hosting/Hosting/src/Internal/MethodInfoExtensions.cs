using System.Reflection;

namespace Microsoft.AspNetCore.Hosting.Internal
{
    internal static class MethodInfoExtensions
    {
        public static TDelegate CreateDelegate<TDelegate>(this MethodInfo methodInfo, object target)
        {
            if (methodInfo.IsStatic)
            {
                return (TDelegate)(object)methodInfo.CreateDelegate(typeof(TDelegate));
            }

            return (TDelegate)(object)methodInfo.CreateDelegate(typeof(TDelegate), target);
        }
    }
}

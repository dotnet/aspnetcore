using System.Reflection;

namespace Microsoft.AspNetCore.Hosting.Internal
{
    internal static class MethodInfoExtensions
    {
        // This version of MethodInfo.Invoke removes TargetInvocationExceptions
        public static object InvokeWithoutWrappingExceptions(this MethodInfo methodInfo, object obj, object[] parameters)
        {
            // These are the default arguments passed when methodInfo.Invoke(obj, parameters) are called. We do the same
            // here but specify BindingFlags.DoNotWrapExceptions to avoid getting TAE (TargetInvocationException)
            // methodInfo.Invoke(obj, BindingFlags.Default, binder: null, parameters: parameters, culture: null)

            return methodInfo.Invoke(obj, BindingFlags.DoNotWrapExceptions, binder: null, parameters: parameters, culture: null);
        }
    }
}

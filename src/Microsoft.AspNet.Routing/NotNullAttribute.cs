using System;

namespace Microsoft.AspNet.Routing
{
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    internal sealed class NotNullAttribute : Attribute
    {
    }
}

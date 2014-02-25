using System;

namespace Microsoft.AspNet.Mvc
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class FromBodyAttribute : Attribute
    {
    }
}

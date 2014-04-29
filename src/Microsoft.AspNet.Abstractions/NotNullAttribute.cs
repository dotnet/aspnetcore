using System;

namespace Microsoft.AspNet.Abstractions
{
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    internal sealed class NotNullAttribute : Attribute
    {
    }
}
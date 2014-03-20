using System;
using Microsoft.AspNet.Mvc.Filters;

namespace Microsoft.AspNet.Mvc
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class AllowAnonymousAttribute : Attribute, IAllowAnonymous
    {
    }
}

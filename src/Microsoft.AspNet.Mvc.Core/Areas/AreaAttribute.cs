using System;

namespace Microsoft.AspNet.Mvc
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class AreaAttribute : RouteConstraintAttribute
    {
        public AreaAttribute(string areaName)
            : base("area", areaName, blockNonAttributedActions: true)
        {
            if (string.IsNullOrEmpty(areaName))
            {
                throw new ArgumentException("Area name must not be empty", "areaName");
            }
        }
    }
}

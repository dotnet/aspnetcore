
using System;

namespace Microsoft.AspNet.Mvc
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class ViewComponentAttribute : Attribute
    {
        public string Name { get; set; }
    }
}

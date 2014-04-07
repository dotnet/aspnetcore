using System;

namespace Microsoft.AspNet.Mvc
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class ActionNameAttribute : Attribute
    {
        public ActionNameAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; private set; }
    }
}
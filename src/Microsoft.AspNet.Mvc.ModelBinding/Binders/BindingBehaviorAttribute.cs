using System;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class BindingBehaviorAttribute : Attribute
    {
        public BindingBehaviorAttribute(BindingBehavior behavior)
        {
            Behavior = behavior;
        }

        public BindingBehavior Behavior { get; private set; }
    }
}

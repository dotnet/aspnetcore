// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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

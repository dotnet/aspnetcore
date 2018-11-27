// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace ApplicationModelWebSite
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class CloneActionAttribute : Attribute
    {
        public CloneActionAttribute(string newActionName)
        {
            ActionName = newActionName;
        }

        public string ActionName { get; private set; }
    }
}

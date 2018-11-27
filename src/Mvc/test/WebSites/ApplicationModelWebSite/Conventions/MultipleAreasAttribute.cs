// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;

namespace ApplicationModelWebSite
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class MultipleAreasAttribute : Attribute
    {
        public MultipleAreasAttribute(string area1, string area2, params string[] areaNames)
        {
            AreaNames = new string[] { area1, area2 }.Concat(areaNames).ToArray();
        }

        public string[] AreaNames { get; }
    }
}

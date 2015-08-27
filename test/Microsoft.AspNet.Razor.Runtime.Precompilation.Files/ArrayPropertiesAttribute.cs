// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Razor.Runtime.Precompilation
{
    public class ArrayPropertiesAttribute : Attribute
    {
        public Type[] ArrayOfTypes { get; set; }

        public int[] ArrayOfInts { get; set; }

        public DayOfWeek[] Days { get; set; }
    }
}

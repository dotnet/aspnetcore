// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Razor.Runtime.Precompilation
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class AttributesWithArrayConstructorArgumentsAttribute : Attribute
    {
        public AttributesWithArrayConstructorArgumentsAttribute(string[] stringArgs, int[] intArgs)
        {
            StringArgs = stringArgs;
            IntArgs = intArgs;
        }

        public AttributesWithArrayConstructorArgumentsAttribute(string[] stringArgs, Type[] typeArgs, int[] intArgs)
        {
            StringArgs = stringArgs;
            TypeArgs = typeArgs;
            IntArgs = intArgs;
        }

        public AttributesWithArrayConstructorArgumentsAttribute(int[] intArgs, Type[] typeArgs)
        {
            IntArgs = intArgs;
            TypeArgs = typeArgs;
        }

        public string[] StringArgs { get; }

        public Type[] TypeArgs { get; }

        public int[] IntArgs { get; }
    }
}

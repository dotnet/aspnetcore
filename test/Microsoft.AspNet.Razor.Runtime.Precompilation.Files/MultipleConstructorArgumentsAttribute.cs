// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Razor.Runtime.Precompilation
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class MultipleConstructorArgumentsAttribute : Attribute
    {
        public MultipleConstructorArgumentsAttribute(string firstArgument, string secondArgument, int thirdArgument)
        {
            FirstArgument = firstArgument;
            SecondArgument = secondArgument;
            ThirdArgument = thirdArgument;
        }

        public string FirstArgument { get; }

        public string SecondArgument { get; }

        public int ThirdArgument { get; }
    }
}

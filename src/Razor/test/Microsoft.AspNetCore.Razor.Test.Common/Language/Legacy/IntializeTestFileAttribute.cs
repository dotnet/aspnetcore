// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Reflection;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    public class IntializeTestFileAttribute : BeforeAfterTestAttribute
    {
        public override void Before(MethodInfo methodUnderTest)
        {
            if (typeof(ParserTestBase).GetTypeInfo().IsAssignableFrom(methodUnderTest.DeclaringType.GetTypeInfo()))
            {
                var typeName = methodUnderTest.DeclaringType.Name;
                ParserTestBase.FileName = $"TestFiles/ParserTests/{typeName}/{methodUnderTest.Name}";
                ParserTestBase.IsTheory = false;

                if (methodUnderTest.GetCustomAttributes(typeof(TheoryAttribute), inherit: false).Length > 0)
                {
                    ParserTestBase.IsTheory = true;
                }
            }
        }

        public override void After(MethodInfo methodUnderTest)
        {
            if (typeof(ParserTestBase).GetTypeInfo().IsAssignableFrom(methodUnderTest.DeclaringType.GetTypeInfo()))
            {
                ParserTestBase.FileName = null;
                ParserTestBase.IsTheory = false;
            }
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

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

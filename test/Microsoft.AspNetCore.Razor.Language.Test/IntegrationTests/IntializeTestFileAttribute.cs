// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Reflection;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests
{
    public class IntializeTestFileAttribute : BeforeAfterTestAttribute
    {
        public override void Before(MethodInfo methodUnderTest)
        {
            if (typeof(IntegrationTestBase).IsAssignableFrom(methodUnderTest.DeclaringType))
            {
                var typeName = methodUnderTest.DeclaringType.Name;
                IntegrationTestBase.Filename = $"TestFiles/IntegrationTests/{typeName}/{methodUnderTest.Name}";
            }
        }

        public override void After(MethodInfo methodUnderTest)
        {
            if (typeof(IntegrationTestBase).IsAssignableFrom(methodUnderTest.DeclaringType))
            {
                IntegrationTestBase.Filename = null;
            }
        }
    }
}

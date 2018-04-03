// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Reflection;
using Microsoft.AspNetCore.Blazor.Build.Test;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests
{
    public class IntializeTestFileAttribute : BeforeAfterTestAttribute
    {
        public override void Before(MethodInfo methodUnderTest)
        {
            if (typeof(RazorBaselineIntegrationTestBase).GetTypeInfo().IsAssignableFrom(methodUnderTest.DeclaringType.GetTypeInfo()))
            {
                var typeName = methodUnderTest.DeclaringType.Name;
                RazorBaselineIntegrationTestBase.DirectoryPath = $"TestFiles/{typeName}/{methodUnderTest.Name}";
            }
        }

        public override void After(MethodInfo methodUnderTest)
        {
            if (typeof(RazorBaselineIntegrationTestBase).GetTypeInfo().IsAssignableFrom(methodUnderTest.DeclaringType.GetTypeInfo()))
            {
                RazorBaselineIntegrationTestBase.DirectoryPath = null;
            }
        }
    }
}

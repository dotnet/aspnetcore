// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Reflection;
using Microsoft.AspNetCore.Components.Build.Test;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests
{
    public class InitializeTestFileAttribute : BeforeAfterTestAttribute
    {
        public override void Before(MethodInfo methodUnderTest)
        {
            if (typeof(RazorBaselineIntegrationTestBase).GetTypeInfo().IsAssignableFrom(methodUnderTest.ReflectedType.GetTypeInfo()))
            {
                var typeName = methodUnderTest.ReflectedType.Name;
                RazorBaselineIntegrationTestBase.DirectoryPath = $"TestFiles/{typeName}/{methodUnderTest.Name}";
            }
        }

        public override void After(MethodInfo methodUnderTest)
        {
            if (typeof(RazorBaselineIntegrationTestBase).GetTypeInfo().IsAssignableFrom(methodUnderTest.ReflectedType.GetTypeInfo()))
            {
                RazorBaselineIntegrationTestBase.DirectoryPath = null;
            }
        }
    }
}

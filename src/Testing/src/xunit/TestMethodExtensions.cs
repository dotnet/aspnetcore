// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Testing
{
    public static class TestMethodExtensions
    {
        public static string EvaluateSkipConditions(this ITestMethod testMethod)
        {
            var testClass = testMethod.TestClass.Class;
            var assembly = testMethod.TestClass.TestCollection.TestAssembly.Assembly;
            var conditionAttributes = testMethod.Method
                .GetCustomAttributes(typeof(ITestCondition))
                .Concat(testClass.GetCustomAttributes(typeof(ITestCondition)))
                .Concat(assembly.GetCustomAttributes(typeof(ITestCondition)))
                .OfType<ReflectionAttributeInfo>()
                .Select(attributeInfo => attributeInfo.Attribute);

            foreach (ITestCondition condition in conditionAttributes)
            {
                if (!condition.IsMet)
                {
                    return condition.SkipReason;
                }
            }

            return null;
        }
    }
}

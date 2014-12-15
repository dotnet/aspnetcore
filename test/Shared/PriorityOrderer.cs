// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Identity.Test
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Xunit.Abstractions;
    using Xunit.Sdk;

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class TestPriorityAttribute : Attribute
    {
        public TestPriorityAttribute(int priority)
        {
            Priority = priority;
        }

        public int Priority { get; private set; }
    }

    public class PriorityOrderer : ITestCaseOrderer
    {
        public IEnumerable<XunitTestCase> OrderTestCases<XunitTestCase>(IEnumerable<XunitTestCase> testCases) where XunitTestCase : ITestCase
        {
            var sortedMethods = new SortedDictionary<int, List<XunitTestCase>>();

            foreach (XunitTestCase testCase in testCases)
            {
                int priority = 0;

                foreach (IAttributeInfo attr in testCase.TestMethod.Method.GetCustomAttributes((typeof(TestPriorityAttribute))))
                    priority = attr.GetNamedArgument<int>("Priority");

                GetOrCreate(sortedMethods, priority).Add(testCase);
            }

            foreach (var list in sortedMethods.Keys.Select(priority => sortedMethods[priority]))
            {
                list.Sort((x, y) => StringComparer.OrdinalIgnoreCase.Compare(x.TestMethod.Method.Name, y.TestMethod.Method.Name));
                foreach (XunitTestCase testCase in list)
                    yield return testCase;
            }
        }

        static TValue GetOrCreate<TKey, TValue>(IDictionary<TKey, TValue> dictionary, TKey key) where TValue : new()
        {
            TValue result;

            if (dictionary.TryGetValue(key, out result)) return result;

            result = new TValue();
            dictionary[key] = result;

            return result;
        }
    }
}
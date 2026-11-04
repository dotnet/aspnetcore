// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Identity.Test;

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit.Abstractions;
using Xunit.Sdk;

/// <summary>
/// Test priority
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class TestPriorityAttribute : Attribute
{
    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="priority"></param>
    public TestPriorityAttribute(int priority)
    {
        Priority = priority;
    }

    /// <summary>
    /// Priority
    /// </summary>
    public int Priority { get; private set; }
}

/// <summary>
/// Used to run tests in order.
/// </summary>
public class PriorityOrderer : ITestCaseOrderer
{
    /// <summary>
    /// Orders tests cases
    /// </summary>
    /// <typeparam name="XunitTestCase"></typeparam>
    /// <param name="testCases"></param>
    /// <returns></returns>
    public IEnumerable<XunitTestCase> OrderTestCases<XunitTestCase>(IEnumerable<XunitTestCase> testCases) where XunitTestCase : ITestCase
    {
        var sortedMethods = new SortedDictionary<int, List<XunitTestCase>>();

        foreach (XunitTestCase testCase in testCases)
        {
            var priority = 0;

            foreach (IAttributeInfo attr in testCase.TestMethod.Method.GetCustomAttributes((typeof(TestPriorityAttribute)).AssemblyQualifiedName))
            {
                priority = attr.GetNamedArgument<int>("Priority");
            }

            GetOrCreate(sortedMethods, priority).Add(testCase);
        }

        foreach (var list in sortedMethods.Keys.Select(priority => sortedMethods[priority]))
        {
            list.Sort((x, y) => StringComparer.OrdinalIgnoreCase.Compare(x.TestMethod.Method.Name, y.TestMethod.Method.Name));
            foreach (XunitTestCase testCase in list)
            {
                yield return testCase;
            }
        }
    }

    static TValue GetOrCreate<TKey, TValue>(IDictionary<TKey, TValue> dictionary, TKey key) where TValue : new()
    {
        if (dictionary.TryGetValue(key, out var result))
        {
            return result;
        }

        result = new TValue();
        dictionary[key] = result;

        return result;
    }
}

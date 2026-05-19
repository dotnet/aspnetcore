// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Moq;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.InternalTesting;

public class TestableAspNetTestAssemblyRunner : AspNetTestAssemblyRunner
{
    private TestableAspNetTestAssemblyRunner(
        ITestAssembly testAssembly,
        IEnumerable<IXunitTestCase> testCases,
        IMessageSink diagnosticMessageSink,
        IMessageSink executionMessageSink,
        ITestFrameworkExecutionOptions executionOptions) : base(
            testAssembly,
            testCases,
            diagnosticMessageSink,
            executionMessageSink,
            executionOptions)
    {
    }

    public static TestableAspNetTestAssemblyRunner Create(Type fixtureType, bool failTestCase = false)
    {
        var assembly = TestableAssembly.Create(fixtureType, failTestCase: failTestCase);
        var testAssembly = GetTestAssembly(assembly);
        var testCase = GetTestCase(assembly, testAssembly);

        return new TestableAspNetTestAssemblyRunner(
            testAssembly,
            new[] { testCase },
            diagnosticMessageSink: new NullMessageSink(),
            executionMessageSink: new NullMessageSink(),
            executionOptions: Mock.Of<ITestFrameworkExecutionOptions>());

        // Do not call Xunit.Sdk.Reflector.Wrap(assembly) because it uses GetTypes() and that method
        // throws NotSupportedException for a dynamic assembly.
        IAssemblyInfo GetAssemblyInfo(Assembly assembly)
        {
            var testAssemblyName = assembly.GetName().Name;
            var assemblyInfo = new Mock<IReflectionAssemblyInfo>();
            assemblyInfo.SetupGet(r => r.Assembly).Returns(assembly);
            assemblyInfo.SetupGet(r => r.Name).Returns(testAssemblyName);
            assemblyInfo
                .SetupGet(r => r.AssemblyPath)
                .Returns(Path.Combine(Directory.GetCurrentDirectory(), $"{testAssemblyName}.dll"));

            foreach (var attribute in CustomAttributeData.GetCustomAttributes(assembly))
            {
                var attributeInfo = Reflector.Wrap(attribute);
                var attributeName = attribute.AttributeType.AssemblyQualifiedName;
                assemblyInfo
                    .Setup(r => r.GetCustomAttributes(attributeName))
                    .Returns(new[] { attributeInfo });
            }

            var typeInfo = Reflector.Wrap(assembly.GetType(TestableAssembly.TestClassName));
            assemblyInfo.Setup(r => r.GetType(TestableAssembly.TestClassName)).Returns(typeInfo);
            assemblyInfo.Setup(r => r.GetTypes(It.IsAny<bool>())).Returns(new[] { typeInfo });

            return assemblyInfo.Object;
        }

        ITestAssembly GetTestAssembly(Assembly assembly)
        {
            var assemblyInfo = GetAssemblyInfo(assembly);

            return new TestAssembly(assemblyInfo);
        }

        IXunitTestCase GetTestCase(Assembly assembly, ITestAssembly testAssembly)
        {
            var testAssemblyName = assembly.GetName().Name;
            var testCollection = new TestCollection(
                testAssembly,
                collectionDefinition: null,
                displayName: $"Mock collection for '{testAssemblyName}'.");

            var type = assembly.GetType(TestableAssembly.TestClassName);
            var testClass = new TestClass(testCollection, Reflector.Wrap(type));
            var method = type.GetMethod(TestableAssembly.TestMethodName);
            var methodInfo = Reflector.Wrap(method);
            var testMethod = new TestMethod(testClass, methodInfo);

            return new XunitTestCase(
                diagnosticMessageSink: new NullMessageSink(),
                defaultMethodDisplay: TestMethodDisplay.ClassAndMethod,
                defaultMethodDisplayOptions: TestMethodDisplayOptions.None,
                testMethod: testMethod);
        }
    }

    public Task AfterTestAssemblyStartingAsync_Public()
    {
        return base.AfterTestAssemblyStartingAsync();
    }
}

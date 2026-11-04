// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.InternalTesting;

/// <summary>
/// Provides access to contextual information about the running tests. Get access by
/// implementing <see cref="ITestMethodLifecycle"/>.
/// </summary>
/// <remarks>
/// Requires defining <see cref="AspNetTestFramework"/> as the test framework.
/// </remarks>
public sealed class TestContext
{
    private readonly Lazy<TestFileOutputContext> _files;

    public TestContext(
        Type testClass,
        object[] constructorArguments,
        MethodInfo testMethod,
        object[] methodArguments,
        ITestOutputHelper output)
    {
        TestClass = testClass;
        ConstructorArguments = constructorArguments;
        TestMethod = testMethod;
        MethodArguments = methodArguments;
        Output = output;

        _files = new Lazy<TestFileOutputContext>(() => new TestFileOutputContext(this));
    }

    public Type TestClass { get; }
    public MethodInfo TestMethod { get; }
    public object[] ConstructorArguments { get; }
    public object[] MethodArguments { get; }
    public ITestOutputHelper Output { get; }
    public TestFileOutputContext FileOutput => _files.Value;
}

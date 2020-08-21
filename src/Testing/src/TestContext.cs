// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Testing
{
    /// <summary>
    /// Provides access to contextual information about the running tests. Get access by
    /// implementing <see cref="ITestMethodLifecycle"/>.
    /// </summary>
    /// <remarks>
    /// Requires defining <see cref="AspNetTestFramework"/> as the test framework.
    /// </remarks>
    public sealed class TestContext
    {
        private Lazy<TestFileOutputContext> _files;

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
}

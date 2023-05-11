// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.ObjectPool;

namespace Microsoft.Extensions.ObjectPool.TestResources;

public class TestClass : IResettable, IDisposable, ITestClass
{
    public int ResetCalled { get; private set; }
    public int DisposedCalled { get; private set; }
    private readonly TestDependency _testClass;

    public TestClass(TestDependency testClass)
    {
        _testClass = testClass;
    }

    public string ReadMessage() => _testClass.ReadMessage();

    public bool TryReset()
    {
        ResetCalled++;
        return true;
    }

    public void Dispose()
    {
        DisposedCalled++;
    }
}

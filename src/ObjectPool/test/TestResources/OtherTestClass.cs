// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.ObjectPool.TestResources;

public class OtherTestClass : ITestClass
{
    int ITestClass.ResetCalled => throw new NotImplementedException();

    int ITestClass.DisposedCalled => throw new NotImplementedException();

    string ITestClass.ReadMessage()
    {
        throw new NotImplementedException();
    }
}

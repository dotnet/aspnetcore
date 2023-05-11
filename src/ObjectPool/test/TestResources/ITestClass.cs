// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.ObjectPool.TestResources;

public interface ITestClass
{
    int ResetCalled { get; }
    int DisposedCalled { get; }
    string ReadMessage();
}

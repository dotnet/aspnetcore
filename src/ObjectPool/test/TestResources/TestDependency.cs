// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.ObjectPool.TestResources;

public class TestDependency
{
    public const string Message = "I'm here!";

    public string ReadMessage() => Message;
}

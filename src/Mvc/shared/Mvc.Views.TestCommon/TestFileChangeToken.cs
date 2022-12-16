// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Primitives;

public class TestFileChangeToken : IChangeToken
{
    public TestFileChangeToken(string filter = "")
    {
        Filter = filter;
    }

    public bool ActiveChangeCallbacks => false;

    public bool HasChanged { get; set; }

    public string Filter { get; }

    public IDisposable RegisterChangeCallback(Action<object> callback, object state)
    {
        return new NullDisposable();
    }

    private sealed class NullDisposable : IDisposable
    {
        public void Dispose()
        {
        }
    }

    public override string ToString() => Filter;
}

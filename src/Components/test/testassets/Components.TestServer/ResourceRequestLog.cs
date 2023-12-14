// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace TestServer;

public class ResourceRequestLog
{
    private readonly List<string> _requestPaths = new List<string>();

    public IReadOnlyCollection<string> RequestPaths => _requestPaths;

    public void AddRequest(HttpRequest request)
    {
        _requestPaths.Add(request.Path);
    }

    public void Clear()
    {
        _requestPaths.Clear();
    }
}

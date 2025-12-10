// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;

namespace HostedInAspNet.Server;

public class BootResourceRequestLog
{
    private readonly ConcurrentBag<string> _requestPaths = new ConcurrentBag<string>();

    public IReadOnlyCollection<string> RequestPathsWithNewContent => _requestPaths;

    public void AddRequest(string originalRequestPath, HttpResponse response)
    {
        if (response.StatusCode != StatusCodes.Status304NotModified)
        {
            _requestPaths.Add(originalRequestPath);
        }
    }

    public void Clear()
    {
        _requestPaths.Clear();
    }
}

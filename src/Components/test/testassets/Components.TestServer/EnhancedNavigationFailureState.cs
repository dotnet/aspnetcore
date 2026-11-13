// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;

namespace TestServer;

public sealed class EnhancedNavigationFailureState
{
    private readonly ConcurrentDictionary<string, RequestState> _requests = new();

    public RequestState Get(string id) => _requests.GetOrAdd(id, _ => new RequestState());

    public void Remove(string id) => _requests.TryRemove(id, out _);

    public sealed class RequestState
    {
        private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _enhancedRequests;
        private int _fullPageRequests;
        private int _postRequests;

        public int EnhancedRequests => Volatile.Read(ref _enhancedRequests);
        public int FullPageRequests => Volatile.Read(ref _fullPageRequests);
        public int PostRequests => Volatile.Read(ref _postRequests);

        public void RecordRequest(bool enhanced, bool isPost)
        {
            if (enhanced)
            {
                Interlocked.Increment(ref _enhancedRequests);
            }
            else
            {
                Interlocked.Increment(ref _fullPageRequests);
            }

            if (isPost)
            {
                Interlocked.Increment(ref _postRequests);
            }
        }

        public Task WaitAsync(CancellationToken cancellationToken) => _release.Task.WaitAsync(cancellationToken);

        public void Release() => _release.TrySetResult();
    }
}

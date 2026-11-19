// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace BasicTestApp;

// Interactive Server only: holds back the next inbound circuit activity, so a later message from JS can overtake it.
public sealed class InboundActivityDelay
{
    private int _nextDelayMilliseconds;

    public void DelayNext(int milliseconds) => Volatile.Write(ref _nextDelayMilliseconds, milliseconds);

    public int TakeNextDelay() => Interlocked.Exchange(ref _nextDelayMilliseconds, 0);
}

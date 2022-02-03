// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Microsoft.AspNetCore.Mvc;

public class ActivityReplacer : IDisposable
{
    private readonly Activity _activity;

    public ActivityReplacer()
    {
        _activity = new Activity("Test");
        _activity.Start();
    }

    public void Dispose()
    {
        Debug.Assert(Activity.Current == _activity);
        _activity.Stop();
    }
}

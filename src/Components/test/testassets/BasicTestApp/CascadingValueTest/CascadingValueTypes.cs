// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace BasicTestApp.CascadingValueTest;

public class CounterDTO
{
    public int NumClicks { get; set; }

    public void IncrementCount()
    {
        NumClicks++;
    }
}

public interface ICanDecrement
{
    void DecrementCount();
}

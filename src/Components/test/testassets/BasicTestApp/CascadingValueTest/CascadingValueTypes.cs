// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace BasicTestApp.CascadingValueTest
{
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
}

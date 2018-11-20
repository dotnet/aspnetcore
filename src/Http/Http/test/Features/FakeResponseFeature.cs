// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Http.Features
{
    public class FakeResponseFeature : HttpResponseFeature
    {
        List<Tuple<Func<object, Task>, object>> _onCompletedCallbacks = new List<Tuple<Func<object, Task>, object>>();

        public override void OnCompleted(Func<object, Task> callback, object state)
        {
            _onCompletedCallbacks.Add(new Tuple<Func<object, Task>, object>(callback, state));
        }

        public async Task CompleteAsync()
        {
            var callbacks = _onCompletedCallbacks;
            _onCompletedCallbacks = null;
            foreach (var callback in callbacks)
            {
                await callback.Item1(callback.Item2);
            }
        }
    }
}

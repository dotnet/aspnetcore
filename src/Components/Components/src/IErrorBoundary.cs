// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Components
{
    public interface IErrorBoundary
    {
        // Warning: if you make an IErrorBoundary that reacts to errors by rendering some child
        // that throws during its own rendering, you will have an infinite loop of errors. How could
        // we detect this? As long as it's legit to recover, then you have to deal with a succession
        // of valid (non-infinite-looping) errors over time. We can't detect this by looking at the
        // call stack, because a child might trigger an error asynchronously, making it into a slightly
        // spaced out but still infinite loop.
        //
        // It would be weird, but we could track the timestamp of the last error, and if another one
        // occurs in < 100ms, then treat it as fatal. Technically we could do this for all IErrorBoundary
        // (not just the .Web one) by stashing that info in ComponentState, at the cost of using more
        // memory for every component, not just IErrorBoundary.
        //
        // Another option is to handle this in the .Web one using the rule "if you notify me of an error
        // while I believe I'm already showing an error, then I'll just rethrow so it gets treated as
        // fatal for the whole circuit". This would take care of errors caused when rendering ErrorContent.
        // This might be the best solution. People implementing a custom IErrorBoundary should take care
        // to put in similar logic, if we're OK relying on that.
        void HandleException(Exception exception);
    }
}

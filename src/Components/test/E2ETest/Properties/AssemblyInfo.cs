// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

// Parallelism causes issues with the server-side execution tests. It's not clear why, since
// we have separate browser and server instances for different test collections (i.e., classes)
// so they should be entirely isolated anyway. When parallelism is on, we can observe intermittent
// interference between test classes, for example test A observing the UI state of test B, which
// suggests that either Selenium has thread-safety issues that direct commands to the wrong
// browser instance, or something in our tracking of browser instances goes wrong.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

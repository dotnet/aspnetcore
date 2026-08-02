// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
#if MACOS
using Xunit;
#endif

[assembly: ShortClassName]
[assembly: LogLevel(LogLevel.Trace)]
#if MACOS
[assembly: CollectionBehavior(DisableTestParallelization = true)]
#endif

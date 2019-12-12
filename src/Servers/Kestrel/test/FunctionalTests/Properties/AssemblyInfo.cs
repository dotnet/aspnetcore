// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Testing;
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

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

[assembly: ShortClassName]
[assembly: LogLevel(LogLevel.Trace)]
// AddressRegistrationTests can cause issues with other tests so run all tests in sequence.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

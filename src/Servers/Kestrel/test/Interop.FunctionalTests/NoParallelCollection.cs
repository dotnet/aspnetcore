// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Interop.FunctionalTests;

// Define test collection for tests to avoid all other tests.
// Parallelization disable for QUIC test to avoid test flakiness from msquic refusing connections
// because of high resource usage. See https://github.com/dotnet/runtime/issues/55979
[CollectionDefinition(nameof(NoParallelCollection), DisableParallelization = true)]
public partial class NoParallelCollection { }

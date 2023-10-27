// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.TestUtils.Tracing;

// This file comes from Microsoft.AspNetCore.TestUtils and has to be defined in the test assembly.
// It enables EventSourceTestBase's parallel isolation functionality.

[Xunit.CollectionDefinition(EventSourceTestBase.CollectionName, DisableParallelization = true)]
public class EventSourceTestCollection
{
}

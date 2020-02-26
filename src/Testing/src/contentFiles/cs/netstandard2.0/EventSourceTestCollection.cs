// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Testing.Tracing
{
    // This file comes from Microsoft.AspNetCore.Testing and has to be defined in the test assembly.
    // It enables EventSourceTestBase's parallel isolation functionality.

    [Xunit.CollectionDefinition(EventSourceTestBase.CollectionName, DisableParallelization = true)]
    public class EventSourceTestCollection
    {
    }
}

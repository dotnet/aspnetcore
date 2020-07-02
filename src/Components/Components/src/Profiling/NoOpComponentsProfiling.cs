// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Components.Profiling
{
    internal class NoOpComponentsProfiling : ComponentsProfiling
    {
        public override void Start(string? name)
        {
        }

        public override void End(string? name)
        {
        }
    }
}

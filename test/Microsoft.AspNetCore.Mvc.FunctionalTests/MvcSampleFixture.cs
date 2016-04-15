// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    public class MvcSampleFixture<TStartup> : MvcTestFixture<TStartup>
    {
        public MvcSampleFixture()
#if NET451
            : base(Path.Combine("..", "..", "..", "..", "..", "..", "samples"))
#else
            : base(Path.Combine("..", "..", "..", "..", "..", "samples"))
#endif
        {
        }
    }
}

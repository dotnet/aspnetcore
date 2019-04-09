// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.Logging.Testing
{
    public class RepeatContext
    {
        internal int Limit { get; set; }

        internal int CurrentIteration { get; set; }
    }
}

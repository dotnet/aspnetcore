// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Dispatcher
{
    public class DispatcherOptions
    {
        public MatcherCollection Matchers { get; } = new MatcherCollection();

        public IDictionary<string, Type> ConstraintMap = new Dictionary<string, Type>();
    }
}

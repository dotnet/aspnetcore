// Copyright(c) .NET Foundation.All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace TriageBuildFailures.VSTS.Models
{
    public class VSTSArray<T> where T : class
    {
        public int Count { get; set; }
        public IEnumerable<T> Value { get; set; }
    }
}

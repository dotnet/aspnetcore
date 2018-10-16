// Copyright(c) .NET Foundation.All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace TriageBuildFailures.VSTS.Models
{
    public class Area
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public Uri Url { get; set; }
    }
}
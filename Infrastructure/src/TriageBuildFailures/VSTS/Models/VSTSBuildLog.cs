// Copyright(c) .NET Foundation.All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace TriageBuildFailures.VSTS.Models
{
    public class VSTSBuildLog
    {
        public DateTime CreatedOn { get; set; }
        public int Id { get; set; }
        public DateTime LastChangedOn { get; set; }
        public int LineCount { get; set; }
        public string Type { get; set; }
        public Uri Url { get; set; }
    }
}

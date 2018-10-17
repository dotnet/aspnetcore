// Copyright(c) .NET Foundation.All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace TriageBuildFailures.VSTS.Models
{
    public class VSTSTestRun
    {
        public DateTime CompletedDate { get; set; }
        public string Name { get; set; }
        public Uri Url { get; set; }
        public bool? IsAutomated { get; set; }
        public string Id { get; set; }
        public VSTSProject Project { get; set; }
    }
}

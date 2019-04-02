// Copyright(c) .NET Foundation.All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using TriageBuildFailures.Abstractions;

namespace TriageBuildFailures.VSTS.Models
{
    public class DeploymentJob
    {
        public string Id { get; set; }
        public string TimelineRecordId { get; set; }
        public string Name { get; set; }
        public DateTime DateStarted { get; set; }
        public DateTime DateEnded { get; set; }
        public BuildStatus TaskStatus { get; set; }
        public int Rank { get; set; }
        public IEnumerable<VSTSIssue> Issues { get; set; }
        public string AgentName { get; set; }
        public string LogUrl { get; set; }
        public Uri LogUri => new Uri(LogUrl);
    }
}

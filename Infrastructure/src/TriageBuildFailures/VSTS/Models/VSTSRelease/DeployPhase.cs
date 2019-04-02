// Copyright(c) .NET Foundation.All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace TriageBuildFailures.VSTS.Models
{
    public class DeployPhase
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int Rank { get; set; }
        public PhaseType PhaseType { get; set; }
        public DeployPhaseStatus Status { get; set; }
        public string RunPlanId { get; set; }
        public IEnumerable<DeploymentJobItem> DeploymentJobs { get; set; }
        public DateTime StartedOn { get; set; }
    }
}

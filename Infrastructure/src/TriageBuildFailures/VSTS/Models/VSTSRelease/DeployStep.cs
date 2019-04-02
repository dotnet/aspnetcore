// Copyright(c) .NET Foundation.All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace TriageBuildFailures.VSTS.Models
{
    public class DeployStep
    {
        public string Id { get; set; }
        public string DeploymentId { get; set; }
        public int Attempt { get; set; }
        public DeployStatus Status { get; set; }
        public IEnumerable<DeployPhase> ReleaseDeployPhases { get; set; }
    }
}

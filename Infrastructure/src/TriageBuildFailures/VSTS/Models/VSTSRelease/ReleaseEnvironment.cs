// Copyright(c) .NET Foundation.All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace TriageBuildFailures.VSTS.Models
{
    public class ReleaseEnvironment
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public EnvironmentStatus Status { get; set; }
        public IEnumerable<DeployStep> DeploySteps { get; set; }
    }
}

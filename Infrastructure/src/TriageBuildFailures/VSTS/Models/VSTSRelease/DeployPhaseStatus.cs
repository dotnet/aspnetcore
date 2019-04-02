// Copyright(c) .NET Foundation.All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace TriageBuildFailures.VSTS.Models
{
    public enum DeployPhaseStatus
    {
        Canceled,
        Cancelling,
        Failed,
        InProgress,
        NotStarted,
        PartiallySucceeded,
        Skipped,
        Succeeded,
        Undefined
    }
}

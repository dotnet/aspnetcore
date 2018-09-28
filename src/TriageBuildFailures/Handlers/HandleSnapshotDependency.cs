// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using TriageBuildFailures.TeamCity;

namespace TriageBuildFailures.Handlers
{
    public class HandleSnapshotDependency : HandleFailureBase
    {
        private const string SnapshotMessage = "The status of the build has been changed to failing because some of the builds it depends on have failed";

        public override bool CanHandleFailure(TeamCityBuild build)
        {
            return TCClient.GetBuildLog(build).Contains(SnapshotMessage);
        }

        public override Task HandleFailure(TeamCityBuild build)
        {
            // There's nothing to do about this, ignore it.
            return Task.CompletedTask;
        }
    }
}

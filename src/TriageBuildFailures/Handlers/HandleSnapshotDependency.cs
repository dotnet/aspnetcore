// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using TriageBuildFailures.Abstractions;
using TriageBuildFailures.TeamCity;

namespace TriageBuildFailures.Handlers
{
    public class HandleSnapshotDependency : HandleFailureBase
    {
        private const string SnapshotMessage = "The status of the build has been changed to failing because some of the builds it depends on have failed";

        public override async Task<bool> CanHandleFailure(ICIBuild build)
        {
            if (build is TeamCityBuild tcBuild)
            {
                var buildLog = await GetClient(build).GetBuildLog(build);
                return buildLog.Contains(SnapshotMessage);
            }
            else
            {
                return false;
            }
        }

        public override Task HandleFailure(ICIBuild build)
        {
            // There's nothing to do about this, ignore it.
            return Task.CompletedTask;
        }
    }
}

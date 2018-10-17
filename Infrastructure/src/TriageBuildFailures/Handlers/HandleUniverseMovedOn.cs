// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TriageBuildFailures.Abstractions;

namespace TriageBuildFailures.Handlers
{
    /// <summary>
    /// If someone commits to Update Universe while the config is in the middle of doing its thing it will fail to push at the end. This is unavoidable, so ignore these failures.
    /// </summary>
    public class HandleUniverseMovedOn : HandleFailureBase
    {
        private IEnumerable<string> UpdateUniverseBuilds = new string[] { "Coherence_UpdateUniverse", "Releases_21Public_UpdateUniverse", "Releases_22xPublic_UpdateUniverse" };
        private const string UniverseMovedOn = "error: failed to push some refs to 'git@github.com:aspnet/Universe.git'";

        public override async Task<bool> CanHandleFailure(ICIBuild build)
        {
            var buildLog = await GetClient(build).GetBuildLog(build);
            return UpdateUniverseBuilds.Contains(build.BuildTypeID) && buildLog.Contains(UniverseMovedOn);
        }

        public override Task HandleFailure(ICIBuild build)
        {
            // There's nothing to be done, ignore it.
            return Task.CompletedTask;
        }
    }
}

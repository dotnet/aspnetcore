// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TriageBuildFailures.TeamCity;

namespace TriageBuildFailures.Handlers
{
    /// <summary>
    /// Do nothing for a select list of projects which are low priority
    /// </summary>
    /// <remarks>
    /// Ideally over time this should go away.
    /// </remarks>
    public class HandleLowValueBuilds : HandleFailureBase
    {
        private IEnumerable<string> LowValueBuilds = new string[] {
            "Releases_21Public_SiteExtensionReproNuGetIssue",
            "Lite_Infrastructure_AspNetCoreModuleSetup",
            // PB test
            "Releases_22xPublic_PbTestUbuntu",
            "Releases_22xPublic_PbTestWindowsServer2012",
            "Releases_22xPublic_PbTestMacOSSierra",
            "Releases_22xPublic_PbTestMacOSHighSierra",
            "Benchmarks",
            // ANCM
            "Setup_Ancm_IISIntegration",
            "Setup_Ancm_SignBinaries" };

        public override bool CanHandleFailure(TeamCityBuild build)
        {
            return LowValueBuilds.Contains(build.BuildTypeID);
        }

        public override Task HandleFailure(TeamCityBuild build)
        {
            // Do nothing. RAAS doesn't care, at least for now.
            return Task.CompletedTask;
        }
    }
}

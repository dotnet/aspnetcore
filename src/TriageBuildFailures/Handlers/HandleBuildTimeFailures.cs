// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using TeamCityApi;

namespace TriageBuildFailures.Handlers
{
    /// <summary>
    /// A select subset of failure types on important builds should immediately notify the runtime engineering alias.
    /// </summary>
    public class HandleBuildTimeFailures : HandleFailureBase
    {
        private IEnumerable<string> BuildTimeErrors = new string[] { "E:	 ", "error NU1603:", "error KRB4005:", "Failed to publish artifacts:", "error :", "The active test run was aborted. Reason:" };

        private IEnumerable<string> Release21SharedFxBuilds = new string[] { "Releases_21Public_CoherencePackageCacheLinux", "Releases_21Public_SharedFxLinuxMusl",
            "Releases_21Public_CoherencePackageCacheOsx", "Releases_21Public_CoherencePackageCacheWin", "Releases_21Public_CoherencePackageCacheWin86", "Releases_21Public_RuntimeStoreInstallers", };
        private IEnumerable<string> Release21MainBuilds = new string[] { "Releases_21Public_UpdateRepos", "Releases_21Public_UpdateUniverse", "Releases_21Public_BuildTools",
            "Releases_21Public_UniverseCoherence", "Releases_21Public_Publish", "Releases_21Public_Signed", "Releases_21Public_Finale", "Releases_21Public_SiteExtension",
            "Releases_21Public_SetupSharedFramework", "Releases_21Public_WindowsHostingBundle" };
        private IEnumerable<string> Release21Builds {
            get
            {
                return Release21MainBuilds.Concat(Release21SharedFxBuilds);
            }
        }

        private IEnumerable<string> DevSharedFxBuilds = new string[] { "Coherence_CoherencePackageCacheLinux", "Coherence_CoherencePackageCacheWin",
            "Coherence_SharedFxLinuxMusl", "Coherence_CoherencePackageCacheOsx", "Coherence_CoherencePackageCacheWin86", "Coherence_RuntimeStoreInstallers" };
        private IEnumerable<string> DevMainBuilds = new string[] { "Lite_Public_DnxTools", "Coherence_UpdateUniverse", "UniverseCoherence",
            "CoherenceSigned", "Coherence_Finale", "Coherence_SiteExtension", "AspNetCore_Publish",
            "Lite_Infrastructure_SetupSharedFramework", "Lite_Infrastructure_WindowsHostingBundle" };

        private IEnumerable<string> DevBuilds {
            get
            {
                return DevMainBuilds.Concat(DevSharedFxBuilds);
            }
        }

        private IEnumerable<string> ImportantBuilds {
            get
            {
                return Release21Builds.Concat(DevBuilds);
            }
        }

        public override bool CanHandleFailure(TeamCityBuild build)
        {
            // "Lite_" builds are per-repo and should never fail. 
            if ((build.BuildTypeID.StartsWith("Lite_") || ImportantBuilds.Contains(build.BuildTypeID)))
            {
                var log = TCClient.GetBuildLog(build);
                var errors = GetErrorsFromLog(log);
                return errors != null && errors.Count() > 0;
            }

            return false;
        }

        public override async Task HandleFailure(TeamCityBuild build)
        {
            var log = TCClient.GetBuildLog(build);
            var errMsgs = GetErrorsFromLog(log);

            var subject = $"{build.BuildName} failed";
            var body = $"{build.BuildName} failed with the following errors:\n {string.Join(Environment.NewLine, errMsgs)}\n {build.WebURL}";
            var to = EmailClient.Config.EngineringAlias;

            await EmailClient.SendEmail(to, subject, body);
        }

        private IEnumerable<string> GetErrorsFromLog(string log)
        {
            var logLines = log.Split(new string[] { "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);
            return logLines.Where(l => BuildTimeErrors.Any(l.Contains));
        }
    }
}

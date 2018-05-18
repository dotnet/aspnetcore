// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TriageBuildFailures.TeamCity;

namespace TriageBuildFailures.Handlers
{
    /// <summary>
    /// A select subset of failure types on important builds should immediately notify the runtime engineering alias.
    /// </summary>
    public class HandleBuildTimeFailures : HandleFailureBase
    {
        private IEnumerable<string> BuildTimeErrors = new string[] { "E:	 ", "error NU1603:", "error KRB4005:", "Failed to publish artifacts:", "error :", "The active test run was aborted. Reason:" };

        public override bool CanHandleFailure(TeamCityBuild build)
        {
            var log = TCClient.GetBuildLog(build);
            var errors = GetErrorsFromLog(log);
            return errors != null && errors.Count() > 0;
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

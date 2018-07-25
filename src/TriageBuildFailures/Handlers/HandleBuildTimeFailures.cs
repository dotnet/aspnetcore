// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TriageBuildFailures.GitHub;
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

        private const string _BrokenBuildLabel = "Broken Build";
        private static readonly IEnumerable<string> _Notifiers = new string[]{ "@Eilon", "@mkArtakMSFT", "@muratg" };

        public override async Task HandleFailure(TeamCityBuild build)
        {
            var log = TCClient.GetBuildLog(build);
            var errMsgs = GetErrorsFromLog(log);
            var owner = TestToRepoMapper.FindOwner(build.BuildName, Reporter);
            var repo = "Coherence-Signed";
            var issuesTask = GHClient.GetIssues(owner, repo);

            var subject = $"{build.BuildName} failed";
            var applicableIssues = GetApplicableIssues(await issuesTask, subject);

            if(applicableIssues.Count() > 0)
            {
                var comment = $"{build.BuildName} [failed again]({build.WebURL}).";
                await GHClient.CreateComment(applicableIssues.First(), comment);
            }
            else{
                var body = $@"{build.BuildName} failed with the following errors:
```
{string.Join(Environment.NewLine, errMsgs)}
```
{build.WebURL}

CC {string.Join( ", ", _Notifiers)}";
                var tags = new List<string>{ _BrokenBuildLabel };

                await GHClient.CreateIssue(owner, repo, subject, body, tags);
            }
        }

        public IEnumerable<GithubIssue> GetApplicableIssues(IEnumerable<GithubIssue> issues, string issueTitle)
        {    
            return issues.Where(i =>
                i.Title.Equals(issueTitle, StringComparison.OrdinalIgnoreCase) &&
                i.Labels.Any(l => l.Name.Equals(_BrokenBuildLabel, StringComparison.OrdinalIgnoreCase)));
        }

        private IEnumerable<string> GetErrorsFromLog(string log)
        {
            var logLines = log.Split(new string[] { "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);
            return logLines.Where(l => BuildTimeErrors.Any(l.Contains));
        }
    }
}

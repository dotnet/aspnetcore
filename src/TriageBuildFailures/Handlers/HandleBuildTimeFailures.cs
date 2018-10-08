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
        private IEnumerable<string> BuildTimeErrors = new string[]
        {
            "E:	 ",
            "error NU1603:",
            "error KRB4005:",
            "Failed to publish artifacts:",
            "error :",
            "The active test run was aborted. Reason:",
        };

        public override bool CanHandleFailure(TeamCityBuild build)
        {
            var log = TCClient.GetBuildLog(build);
            var errors = GetErrorsFromLog(log);
            return errors != null && errors.Count() > 0;
        }

        private const string _BrokenBuildLabel = "Broken Build";
        private static readonly string[] _Notifiers = new string[] { "Eilon", "mkArtakMSFT", "muratg" };

        public override async Task HandleFailure(TeamCityBuild build)
        {
            var log = TCClient.GetBuildLog(build);
            var owner = TestToRepoMapper.FindOwner(build.BuildName);
            var repo = "AspNetCore-Internal";
            var issuesTask = GHClient.GetIssues(owner, repo);

            var subject = $"{build.BuildName} failed";
            var applicableIssues = GetApplicableIssues(await issuesTask, subject);

            if (applicableIssues.Count() > 0)
            {
                await GHClient.CommentOnBuild(build, applicableIssues.First(), build.BuildName);
            }
            else
            {
                var body = $@"{build.BuildName} failed with the following errors:

```
{ConstructErrorSummary(log)}
```

{build.WebURL}

CC {GitHubUtils.GetAtMentions(_Notifiers)}";
                var tags = new List<string> { _BrokenBuildLabel, GitHubUtils.GetBranchLabel(build.BranchName) };

                await GHClient.CreateIssue(owner, repo, subject, body, tags);
            }
        }

        public IEnumerable<GitHubIssue> GetApplicableIssues(IEnumerable<GitHubIssue> issues, string issueTitle)
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

        private string ConstructErrorSummary(string log)
        {
            var errMsgs = GetErrorsFromLog(log);
            var result = string.Join(Environment.NewLine, errMsgs);
            var maxErrSize = GitHubClientWrapper.MaxBodyLength / 2;
            if (result.Length > maxErrSize)
            {
                result = result.Substring(0, maxErrSize);
            }

            return result;
        }
    }
}

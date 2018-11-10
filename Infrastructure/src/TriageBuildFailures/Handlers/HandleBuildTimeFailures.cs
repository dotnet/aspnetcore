// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TriageBuildFailures.Abstractions;
using TriageBuildFailures.GitHub;

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
            "TF400889:", // The following path contains more than the allowed 259 characters
            "Failed to publish artifacts:",
            "error :",
            "The active test run was aborted. Reason:",
            "Attempting to cancel the build...",
            "Build FAILED.",
            "npm ERR!",
            "API rate limit exceeded for user ID"
        };

        public override async Task<bool> CanHandleFailure(ICIBuild build)
        {
            var client = GetClient(build);
            var log = await client.GetBuildLog(build);
            var errors = GetErrorsFromLog(log);
            return errors != null && errors.Count() > 0;
        }

        private const string _BrokenBuildLabel = "Broken Build";
        private static readonly string[] _Notifiers = new string[] { "Eilon", "mkArtakMSFT", "muratg" };

        public override async Task HandleFailure(ICIBuild build)
        {
            var log = GetClient(build).GetBuildLog(build);
            var owner = "aspnet";
            var repo = GitHubUtils.PrivateRepo;
            var issues = await GHClient.GetIssues(owner, repo);

            var subject = $"{build.BuildName} failed";
            var applicableIssues = GetApplicableIssues(issues, subject);

            if (applicableIssues.Count() > 0)
            {
                await GHClient.CommentOnBuild(build, applicableIssues.First(), build.BuildName);
            }
            else
            {
                var body = $@"{build.BuildName} failed with the following errors:

```
{ConstructErrorSummary(await log)}
```

{build.WebURL}

CC {GitHubUtils.GetAtMentions(_Notifiers)}";
                var issueLabels = new[]
                {
                    _BrokenBuildLabel,
                    GitHubUtils.GetBranchLabel(build.Branch)
                };

                // TODO: Would be nice if we could figure out how to map broken builds to failure areas
                await GHClient.CreateIssue(owner, repo, subject, body, issueLabels, assignees: null);
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

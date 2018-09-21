// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using TriageBuildFailures.Email;
using TriageBuildFailures.GitHub;
using TriageBuildFailures.TeamCity;

namespace TriageBuildFailures.Handlers
{
    /// <summary>
    /// A base class for FailureHandlers.
    /// </summary>
    public abstract class HandleFailureBase : IFailureHandler
    {
        public Config Config { get; set; }
        public TeamCityClientWrapper TCClient { get; set; }
        public GitHubClientWrapper GHClient { get; set; }
        public EmailClient EmailClient { get; set; }
        public IReporter Reporter { get; set; }

        public abstract bool CanHandleFailure(TeamCityBuild build);
        public abstract Task HandleFailure(TeamCityBuild build);

        protected async Task CommentOnTest(TeamCityBuild build, GithubIssue issue, List<string> testNames)
        {
            var (commentsAboutThisBuild, commentsFromToday) = await GatherComments(build, issue);
            if (commentsAboutThisBuild.Count() == 0)
            {
                var count = testNames.Count;
                var comment = $"There were {count} failures [with about the same error]({build.WebURL}) on {build.BranchName}:\n";
                var testOutputLimit = 10;
                for (var i = 0; i < testOutputLimit && i < count; ++i)
                {
                    comment += $"- {testNames[i]}\n";
                }
                if (count > testOutputLimit)
                {
                    comment += $"- (And {count - testOutputLimit} more test failures in this build)\n";
                }
                if (commentsFromToday.Count() == 0)
                {
                    await GHClient.CreateComment(issue, comment);
                }
                else
                {
                    var todaysComment = commentsFromToday.First();
                    var newBody = $"{todaysComment.Body}\n{comment}";
                    await GHClient.EditComment(issue, todaysComment, newBody);
                }
            }

            var branchLabel = BranchLabel(build.BranchName);
            if (!issue.Labels.Any(l => l.Name.Equals(branchLabel, StringComparison.OrdinalIgnoreCase)))
            {
                await GHClient.AddLabel(issue, branchLabel);
            }
        }

        protected async Task CommentOnBuild(TeamCityBuild build, GithubIssue issue, string buildName)
        {
            var (commentsAboutThisBuild, commentsFromToday) = await GatherComments(build, issue);
            if (commentsAboutThisBuild.Count() == 0)
            {
                var comment = $"{buildName} [failed with about the same error]({build.WebURL}) on {build.BranchName}.";
                if (commentsFromToday.Count() == 0)
                {
                    await GHClient.CreateComment(issue, comment);
                }
                else
                {
                    var todaysComment = commentsFromToday.First();
                    var newBody = $"{todaysComment.Body}\n{comment}";
                    await GHClient.EditComment(issue, todaysComment, newBody);
                }
            }

            var branchLabel = BranchLabel(build.BranchName);
            if (!issue.Labels.Any(l => l.Name.Equals(branchLabel, StringComparison.OrdinalIgnoreCase)))
            {
                await GHClient.AddLabel(issue, branchLabel);
            }
        }

        // Gathers issue comments and adds appropriate labels
        private async Task<(IEnumerable<Octokit.IssueComment> commentsAboutThisBuild, IEnumerable<Octokit.IssueComment> commentsFromToday)>
            GatherComments(TeamCityBuild build, GithubIssue issue)
        {
            var comments = await GHClient.GetIssueComments(issue);
            var commentsFromToday = comments.Where(c =>
                c.CreatedAt.Date == DateTime.UtcNow.Date
                && c.User.Login == Config.GitHub.BotUsername
                && c.Body.Contains("This comment was made automatically")
                && !c.Body.StartsWith("Please use this workflow"));
            var commentsAboutThisBuild = comments.Where(c => c.Body.Contains(build.WebURL.ToString()));

            return (commentsAboutThisBuild, commentsFromToday);
        }

        public static string BranchLabel(string branchName)
        {
            return $"Branch:{branchName}";
        }
    }
}

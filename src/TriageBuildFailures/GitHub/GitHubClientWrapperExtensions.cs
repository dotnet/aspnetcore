// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Octokit;
using TriageBuildFailures.Abstractions;

namespace TriageBuildFailures.GitHub
{
    public static class GitHubClientWrapperExtensions
    {
        public static async Task CommentOnTest(this GitHubClientWrapper gitHubClient, ICIBuild build, GitHubIssue issue, List<string> testNames)
        {
            var (commentsAboutThisBuild, commentsFromToday) = await gitHubClient.GatherComments(build, issue);
            if (!commentsAboutThisBuild.Any())
            {
                var count = testNames.Count;
                var newComment = $"There were {count} failures [with about the same error]({build.WebURL}) on {build.Branch} at {build.StartDate.ToString("T")}:\n";
                var testOutputLimit = 10;
                for (var i = 0; i < testOutputLimit && i < count; ++i)
                {
                    newComment += $"- {testNames[i]}\n";
                }
                if (count > testOutputLimit)
                {
                    newComment += $"- (And {count - testOutputLimit} more test failures in this build)\n";
                }
                if (commentsFromToday.Count() == 0)
                {
                    await gitHubClient.CreateComment(issue, newComment);
                }
                else
                {
                    var todaysComment = commentsFromToday.First();
                    var newBody = $"{todaysComment.Body}\n{newComment}";
                    await gitHubClient.EditComment(issue, todaysComment, newBody);
                }
            }

            var branchLabel = GitHubUtils.GetBranchLabel(build.Branch);
            if (!issue.Labels.Any(l => l.Name.Equals(branchLabel, StringComparison.OrdinalIgnoreCase)))
            {
                await gitHubClient.AddLabel(issue, branchLabel);
            }
        }

        public static async Task CommentOnBuild(this GitHubClientWrapper gitHubClient, ICIBuild build, GitHubIssue issue, string buildName)
        {
            var (commentsAboutThisBuild, commentsFromToday) = await gitHubClient.GatherComments(build, issue);
            if (!commentsAboutThisBuild.Any())
            {
                var comment = $"{buildName} [failed again]({build.WebURL}) on {build.Branch} at {build.StartDate.ToString("T")}.";
                if (commentsFromToday.Count() == 0)
                {
                    await gitHubClient.CreateComment(issue, comment);
                }
                else
                {
                    var todaysComment = commentsFromToday.First();
                    var newBody = $"{todaysComment.Body}\n{comment}";
                    await gitHubClient.EditComment(issue, todaysComment, newBody);
                }
            }

            var branchLabel = GitHubUtils.GetBranchLabel(build.Branch);
            if (!issue.Labels.Any(l => l.Name.Equals(branchLabel, StringComparison.OrdinalIgnoreCase)))
            {
                await gitHubClient.AddLabel(issue, branchLabel);
            }
        }

        // Gathers issue comments and adds appropriate labels
        private static async Task<(IEnumerable<IssueComment> commentsAboutThisBuild, IEnumerable<IssueComment> commentsFromToday)>
            GatherComments(this GitHubClientWrapper gitHubClient, ICIBuild build, GitHubIssue issue)
        {
            var comments = await gitHubClient.GetIssueComments(issue);
            var botCommentsFromToday = comments.Where(c =>
                c.CreatedAt.Date == DateTime.UtcNow.Date
                && c.User.Login == gitHubClient.Config.BotUsername
                && c.Body.Contains("This comment was made automatically")
                && !c.Body.Contains("Please use this workflow"));

            var commentsAboutThisBuild = comments.Where(c => c.Body.Contains(build.WebURL.ToString()));

            return (commentsAboutThisBuild, botCommentsFromToday);
        }
    }
}

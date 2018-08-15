// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
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

        protected async Task CommentOnIssue(TeamCityBuild build, GithubIssue issue, string shortTestName)
        {
            var comments = await GHClient.GetIssueComments(issue);
            var commentsFromToday = comments.Where(c =>
                c.CreatedAt.Date == DateTime.UtcNow.Date
                && c.User.Login == Config.GitHub.BotUsername
                && c.Body.Contains("This comment was made automatically")
                && !c.Body.StartsWith("Please use this workflow"));
            var commentsAboutThisBuild = comments.Where(c => c.Body.Contains(build.WebURL.ToString()));
            if (commentsAboutThisBuild.Count() == 0)
            {
                var comment = $"{shortTestName} [failed with about the same error]({build.WebURL}) on {build.BranchName}.";
                if (commentsFromToday.Count() == 0)
                {
                    await GHClient.CreateComment(issue, comment);
                }
                else
                {
                    var todaysComment = commentsFromToday.First();
                    var newBody = $"{comment}\n{todaysComment.Body}";
                    await GHClient.EditComment(issue, todaysComment, newBody);
                }
            }
            var branchLabel = BranchLabel(build.BranchName);
            if(!issue.Labels.Any(l => l.Name.Equals(branchLabel, StringComparison.OrdinalIgnoreCase)))
            {
                await GHClient.AddLabel(issue, branchLabel);
            }
        }

        private static string BranchLabel(string branchName)
        {
            return $"Branch:{branchName}";
        }
    }
}

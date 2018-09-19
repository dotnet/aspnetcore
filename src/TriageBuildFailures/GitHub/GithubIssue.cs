// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Octokit;

namespace TriageBuildFailures.GitHub
{
    public class GithubIssue : Issue
    {
        public GithubIssue(Issue issue)
            : base(issue.Url, issue.HtmlUrl, issue.CommentsUrl, issue.EventsUrl, issue.Number, issue.State.Value, issue.Title,
                  issue.Body, issue.ClosedBy, issue.User, issue.Labels, issue.Assignee, issue.Assignees, issue.Milestone,
                  issue.Comments, issue.PullRequest, issue.ClosedAt, issue.CreatedAt, issue.UpdatedAt, issue.Id, issue.Locked,
                  issue.Repository)
        {
        }

        public string RepositoryName
        {
            get
            {
                return Repository?.Name == null ? Url.Split('/')[5] : Repository.Name;
            }
        }

        public string RepositoryOwner
        {
            get
            {
                return Repository?.Owner == null ? Url.Split('/')[4] : Repository.Owner.Name;
            }
        }
    }
}

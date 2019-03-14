// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Octokit;

namespace TriageBuildFailures.GitHub
{
    public class GitHubPR : PullRequest
    {
        public GitHubPR(PullRequest pr)
            : base(pr.Id, pr.Url, pr.HtmlUrl, pr.DiffUrl, pr.PatchUrl,
                  pr.IssueUrl, pr.StatusesUrl, pr.Number, pr.State.Value, pr.Title,
                  pr.Body, pr.CreatedAt, pr.UpdatedAt, pr.ClosedAt, pr.MergedAt,
                  pr.Head, pr.Base, pr.User, pr.Assignee, pr.Assignees, pr.Mergeable,
                  pr.MergeableState?.Value, pr.MergedBy, pr.MergeCommitSha, pr.Comments, pr.Commits,
                  pr.Additions, pr.Deletions, pr.ChangedFiles, pr.Milestone, pr.Locked,
                  pr.RequestedReviewers)
        {
        }


        public string RepositoryName
        {
            get
            {
                return Url.Split('/')[5];
            }
        }

        public string RepositoryOwner
        {
            get
            {
                return Url.Split('/')[4];
            }
        }

        public override bool Equals(object obj)
        {
            return obj is GitHubIssue issue &&
                   RepositoryName == issue.RepositoryName &&
                   RepositoryOwner == issue.RepositoryOwner &&
                   Number == issue.Number;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(RepositoryName, RepositoryOwner, Number);
        }
    }
}

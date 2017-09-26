using Octokit;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PullRequestSubmitter.Helpers
{
    static class GitHubUtil
    {
        public static async Task<IDictionary<string, NewBlob>> GetEditsToCommit(
            GitHubClient client, Repository upstreamRepo, string baseSha,
            IEnumerable<PropertyUpdate> propertyUpdates)
        {
            // Find the file to update
            var existingTree = await client.Git.Tree.GetRecursive(upstreamRepo.Id, baseSha);

            // Update the files' contents
            var result = new Dictionary<string, NewBlob>();
            var filesToUpdate = propertyUpdates.GroupBy(p => p.Filename);
            foreach (var fileToUpdate in filesToUpdate)
            {
                var fileContents = await GetFileContentsAsString(
                    client, upstreamRepo, existingTree.Tree, fileToUpdate.Key);

                foreach (var propToUpdate in fileToUpdate)
                {
                    var propName = propToUpdate.PropertyName;
                    var patternToReplace = new Regex($"<{propName}>[^<]+</{propName}>");
                    if (!patternToReplace.IsMatch(fileContents))
                    {
                        throw new Exception($"The file {fileToUpdate.Key} does not contain a match for regex " + patternToReplace.ToString());
                    }

                    fileContents = patternToReplace.Replace(
                        fileContents,
                        $"<{propName}>{propToUpdate.NewValue}</{propName}>");
                }

                var newBlob = new NewBlob { Content = fileContents, Encoding = EncodingType.Utf8 };
                result.Add(fileToUpdate.Key, newBlob);
            }

            return result;
        }

        public static async Task<string> GetLatestCommitSha(
            GitHubClient client, Repository repo, string branchName)
        {
            var commitRef = await client.Git.Reference.Get(
                repo.Id,
                $"heads/{branchName}");
            return commitRef.Object.Sha;
        }

        public static async Task<string> CommitModifiedFiles(
            GitHubClient client, Repository toRepo, string toBranchName, string parentCommitSha,
            IDictionary<string, NewBlob> modifiedFiles, string commitMessage)
        {
            // Build and commit a new tree representing the updated state
            var newTree = new NewTree { BaseTree = parentCommitSha };
            foreach (var kvp in modifiedFiles)
            {
                newTree.Tree.Remove(new NewTreeItem { Path = kvp.Key });
                newTree.Tree.Add(new NewTreeItem()
                {
                    Type = TreeType.Blob,
                    Mode = "100644",
                    Sha = (await client.Git.Blob.Create(toRepo.Id, kvp.Value)).Sha,
                    Path = kvp.Key
                });
            }
            var createdTree = await client.Git.Tree.Create(toRepo.Id, newTree);
            var commit = await client.Git.Commit.Create(
                toRepo.Id,
                new NewCommit(commitMessage, createdTree.Sha, parentCommitSha));

            // Update the target branch to point to the new commit
            await client.Git.Reference.Update(
                toRepo.Id,
                $"heads/{toBranchName}",
                new ReferenceUpdate(commit.Sha, force: true));

            return commit.Sha;
        }

        public static async Task<Issue> FindExistingPullRequestToUpdate(
            GitHubClient client, User currentUser, Repository upstreamRepo,
            Repository forkRepo, string forkBranch)
        {
            // Search for candidate PRs (same author, still open, etc.)
            var fromBaseRef = $"{forkRepo.Owner.Login}:{forkBranch}";
            var searchInRepos = new RepositoryCollection();
            searchInRepos.Add(upstreamRepo.Owner.Login, upstreamRepo.Name);
            var searchRequest = new SearchIssuesRequest
            {
                Repos = searchInRepos,
                Type = IssueTypeQualifier.PullRequest,
                Author = currentUser.Login,
                State = ItemState.Open
            };
            var searchResults = await client.Search.SearchIssues(searchRequest);

            // Of the candidates, find the highest-numbered one that is requesting a
            // pull from the same fork and branch. GitHub only allows there to be one
            // of these at any given time, but we're more likely to find it faster
            // by searching from newest to oldest.
            var candidates = searchResults.Items.OrderByDescending(item => item.Number);
            foreach (var prInfo in candidates)
            {
                var pr = await client.PullRequest.Get(upstreamRepo.Id, prInfo.Number);
                if (pr.Head?.Repository?.Id == forkRepo.Id && pr.Head?.Ref == forkBranch)
                {
                    return prInfo;
                }
            }

            return null;
        }

        public static async Task<PullRequest> CreateNewPullRequest(
            GitHubClient client, Repository upstreamRepo, string upstreamBranch,
            Repository forkRepo, string forkBranch, string prBodyText)
        {
            var fromBaseRef = $"{forkRepo.Owner.Login}:{forkBranch}";
            var newPr = new NewPullRequest(
                prBodyText,
                fromBaseRef,
                upstreamBranch);
            return await client.PullRequest.Create(upstreamRepo.Id, newPr);
        }

        public static async Task UpdateExistingPullRequestTitle(
            GitHubClient client, Repository upstreamRepo, int prNumber, string newTitle)
        {
            var updateInfo = new PullRequestUpdate { Title = newTitle };
            await client.PullRequest.Update(upstreamRepo.Id, prNumber, updateInfo);
        }

        private static async Task<string> GetFileContentsAsString(
            GitHubClient client, Repository repo, IReadOnlyList<TreeItem> tree, string path)
        {
            var existingFile = tree.FirstOrDefault(item => item.Path == path);
            var blob = await client.Git.Blob.Get(repo.Id, existingFile.Sha);

            switch (blob.Encoding.Value)
            {
                case EncodingType.Utf8:
                    return blob.Content;
                case EncodingType.Base64:
                    return Encoding.UTF8.GetString(Convert.FromBase64String(blob.Content));
                default:
                    throw new InvalidDataException($"Unsupported encoding: {blob.Encoding.StringValue}");
            }
        }
    }
}

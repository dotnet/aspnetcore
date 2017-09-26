using Microsoft.Build.Framework;
using Octokit;
using PullRequestSubmitter.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PullRequestSubmitter
{
    public class PullRequestTask : Microsoft.Build.Utilities.Task
    {
        [Required] public string ApiToken { get; set; }
        [Required] public string UpstreamRepoOwner { get; set; }
        [Required] public string UpstreamRepoName { get; set; }
        [Required] public string UpstreamRepoBranch { get; set; }
        [Required] public string ForkRepoName { get; set; }
        [Required] public string ForkRepoBranch { get; set; }
        [Required] public string Message { get; set; }
        [Required] public string FileToUpdate { get; set; }
        [Required] public ITaskItem[] PropertyUpdates { get; set; }

        public override bool Execute()
        {
            return ExecuteAsync().Result;
        }

        private IEnumerable<PropertyUpdate> GetPropertyUpdates()
        {
            return PropertyUpdates.Select(item => new PropertyUpdate
            {
                Filename = FileToUpdate,
                PropertyName = item.ItemSpec,
                NewValue = item.GetMetadata("NewValue")
            });
        }

        private async Task<bool> ExecuteAsync()
        {
            var client = new GitHubClient(new ProductHeaderValue("PullRequestSubmitter"))
            {
                Credentials = new Credentials(ApiToken),
            };

            // Find the upstream repo and determine what edits we want to make
            LogHigh($"Finding upstream repo: {UpstreamRepoOwner}/{UpstreamRepoName}...");
            var upstreamRepo = await client.Repository.Get(UpstreamRepoOwner, UpstreamRepoName);
            var upstreamCommitSha = await GitHubUtil.GetLatestCommitSha(client, upstreamRepo, UpstreamRepoBranch);
            LogHigh($"Found upstream commit to update: {upstreamCommitSha} ({UpstreamRepoBranch})");
            var editsToCommit = await GitHubUtil.GetEditsToCommit(
                client, upstreamRepo, upstreamCommitSha, GetPropertyUpdates());
            if (editsToCommit.Any())
            {
                var filesList = string.Join('\n',
                    editsToCommit.Select(e => " - " + e.Key));
                LogHigh($"Will apply edits to file(s):\n{filesList}");
            }
            else
            {
                Log.LogError("Found no edits to apply. Aborting.");
                return false;
            }

            // Commit the edits into the fork repo, updating its head to point to a new tree
            // formed by updating the tree from the upstream SHA
            var currentUser = await client.User.Current();
            LogHigh($"Finding fork repo: {currentUser.Login}/{ForkRepoName}...");
            var forkRepo = await client.Repository.Get(currentUser.Login, ForkRepoName);
            var newCommitSha = await GitHubUtil.CommitModifiedFiles(
                client,
                forkRepo,
                ForkRepoBranch,
                upstreamCommitSha,
                editsToCommit,
                Message);
            LogHigh($"Committed edits. {currentUser.Login}/{ForkRepoName} branch {ForkRepoBranch} is now at {newCommitSha}");

            // If applicable, submit a new PR
            LogHigh($"Checking if there is already an open PR we can update...");
            var prToUpdate = await GitHubUtil.FindExistingPullRequestToUpdate(
                client, currentUser, upstreamRepo, forkRepo, ForkRepoBranch);
            if (prToUpdate == null)
            {
                LogHigh($"No existing open PR found. Creating new PR...");
                var newPr = await GitHubUtil.CreateNewPullRequest(
                    client, upstreamRepo, UpstreamRepoBranch, forkRepo, ForkRepoBranch, Message);
                LogHigh($"Created pull request #{newPr.Number} at {newPr.HtmlUrl}");
            }
            else
            {
                LogHigh($"Found existing PR #{prToUpdate.Number}. Updating details...");
                await GitHubUtil.UpdateExistingPullRequestTitle(
                    client, upstreamRepo, prToUpdate.Number, Message);
                LogHigh($"Finished updating PR #{prToUpdate.Number} at {prToUpdate.HtmlUrl}");
            }

            return true;
        }

        private void LogHigh(string message)
        {
            Log.LogMessage(MessageImportance.High, message);
        }
    }
}

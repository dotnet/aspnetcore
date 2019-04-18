using Octokit;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;

namespace ConsoleApp9
{
    class Program
    {
        // Ideas for other things to visualize:
        // Pull requests
        // average time for PR open
        // Average time for issue open
        // Average time for issue triaged
        // average time for PR from open reviewed
        private static readonly string _repoOwner = "aspnet";
        private static readonly string _repoName = "aspnetcore";
        private static readonly string _branch = "master";

        static int Main(string[] args)
        {
            var application = new CommandLineApplication();
            application.HelpOption();

            application.OnExecute(async () =>
            {
                await GetGithubStatistics();
            });

            return application.Execute();
        }
        
        private static async Task GetGithubStatistics()
        {
            // Github personal access token so we don't get rate limited.
            var token = Environment.GetEnvironmentVariable("PersonalAccessToken");

            using (var db = new CheckContext())
            {
                try
                {
                    var client = new GitHubClient(new ProductHeaderValue("aspnetcore"));
                    client.Credentials = new Credentials(token);

                    var prRequest = new PullRequestRequest();
                    prRequest.State = ItemStateFilter.All;
                    prRequest.Base = _branch;
                    prRequest.SortDirection = SortDirection.Descending;
                    
                    var pullRequests = await client.PullRequest.GetAllForRepository(_repoOwner, _repoName, prRequest);
                    
                    // Grab the latest 100 PRs made. Enumerating all of them is too slow and 100 PRs 
                    for (var i = 0; i < 100; i++)
                    {
                        await HandlePullRequest(pullRequests[i], client, db);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            Console.WriteLine("Finished grabbing github statistics.");
        }

        private static async Task HandlePullRequest(PullRequest pr, GitHubClient client, CheckContext db)
        {
            // check if pr is pointing to master
            var checks = await client.Check.Run.GetAllForReference(_repoOwner, _repoName, pr.Head.Sha);
            foreach (var checkRun in checks.CheckRuns)
            {
                var status = checkRun.Conclusion;
                if (status != CheckConclusion.Success)
                {
                    continue;
                }

                var start = checkRun.StartedAt;
                var finished = checkRun.CompletedAt;

                var checkTypeModel = await db.CheckTypes.Include(c => c.Checks).FirstOrDefaultAsync(c => c.Name == checkRun.Name);
                
                // CheckType is the type of PR validation, for example Test Windows x64/x86 or Code Check
                // Check is the actual successful check for a PR
                if (checkTypeModel == null)
                {
                    // Add a new check and check type to the db. 
                    var ck = new Check { PullRequestName = pr.Url, SHA = pr.Head.Sha, TimeTaken = (finished.Value - start).TotalMinutes, Start = start, Finished = finished.Value};
                    checkTypeModel = new CheckType { Name = checkRun.Name, Checks = new List<Check>{ ck } };
                    db.CheckTypes.Add(checkTypeModel);
                }
                else
                {
                    // Otherwise check if we have already added the check for a given PR.
                    // TODO this probably should do a check based on SHA instead of PR url, as there can be multiple successful
                    // runs for a PR.
                    var checkModel = await db.Checks.FirstOrDefaultAsync(c => c.PullRequestName == pr.Url && c.CheckType.Name == checkRun.Name);
                    if (checkModel == null)
                    {
                        checkTypeModel.Checks.Add(
                            new Check { PullRequestName = pr.Url, SHA = pr.Head.Sha, TimeTaken = (finished.Value - start).TotalMinutes, Start = start, Finished = finished.Value });
                    }
                }
                db.SaveChanges();
            }
        }
    }
}

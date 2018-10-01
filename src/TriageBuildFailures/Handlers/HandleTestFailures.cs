// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Common;
using Octokit;
using TriageBuildFailures.GitHub;
using TriageBuildFailures.TeamCity;

namespace TriageBuildFailures.Handlers
{
    /// <summary>
    /// When tests fail file an issue about it. If an issue has already been filed comment on the issue so people know it's still happening.
    /// </summary>
    public class HandleTestFailures : HandleFailureBase
    {
        private const string NoStackTraceAvailable = "No stacktrace available";

        public override bool CanHandleFailure(TeamCityBuild build)
        {
            var tests = TCClient.GetTests(build);
            return tests.Any(s => s.Status == BuildStatus.FAILURE);
        }

        private static string SafeGetExceptionMessage(string errors)
        {
            return string.IsNullOrEmpty(errors) ? NoStackTraceAvailable : ErrorParsing.GetExceptionMessage(errors);
        }

        private string TrimTestFailureText(string text)
        {
            var result = text;

            if (result.Length > 6000)
            {
                result = text.Substring(0, 6000);
                result += $"{Environment.NewLine}...";
            }

            return result;
        }

        private const string WorkFlowComment = @"Please use this workflow to address this flaky test issue, including checking applicable checkboxes and filling in the applicable ""TODO"" entries:

* Is this actually a flaky test?
  * No, this is a regular test failure, fix the test/product (TODO: Link to commit/PR)
  * Yes, proceed below...

* Is this test failure caused by product code flakiness? (Either this product, or another product this test depends on.)
  * [ ] File a bug against the product (TODO: Link to other bug)
  * Is it possible to change the test to avoid the flakiness?
    * Yes? Go to the ""Change the test!"" section.
    * No?
      * [ ] Disable the test (TODO: Link to PR/commit)
      * [ ] Wait for other bug to be resolved
      * [ ] Wait for us to get build that has the fix
      * [ ] Re-enable our test (TODO: Link to PR/commit)
      * [ ] Close this bug

* Is it that the test itself is flaky? This includes external transient problems (e.g. remote server problems, file system race condition, etc.)
  * Is there is a way to change our test to avoid this flakiness?
    * Yes? Change the test!
      * [ ] Change the test to avoid the flakiness, for example by using a different test strategy, or by adding retries w/ timeouts (TODO: Link to PR/commit)
      * [ ] Run the test 100 times locally as a sanity check.
      * [ ] Close this bug
    * No?
      * Is there any logging or extra information that we could add to make this more diagnosable when it happens again?
        * Yes?
            * [ ] Add the logging (TODO: Link to PR/commit)
        * No?
            * [ ] Delete the test because flaky tests are not useful (TODO: Link to PR/commit)";

        public override async Task HandleFailure(TeamCityBuild build)
        {
            var tests = TCClient.GetTests(build);
            var failures = tests.Where(s => s.Status == BuildStatus.FAILURE);

            var testAggregates = new Dictionary<GitHubIssue, List<string>>();

            foreach (var failure in failures)
            {
                Reporter.Output($"Inspecting test failure {failure.Name}...");
                var repo = TestToRepoMapper.FindRepo(failure.Name, Reporter);
                var owner = TestToRepoMapper.FindOwner(failure.Name);

                var issuesTask = GHClient.GetFlakyIssues(owner, repo);

                var errors = TCClient.GetTestFailureText(failure);

                var applicableIssues = GetApplicableIssues(await issuesTask, failure);

                var shortTestName = GetTestName(failure);
                if (applicableIssues.Count() == 0)
                {
                    var subject = $"Test failure: {shortTestName}";
                    // TODO: CC area experts
                    var body = $@"This test [failed]({build.WebURL}) with the following error:

```
{TrimTestFailureText(errors)}
```

Other tests within that build may have failed with a similar message, but they are not listed here. Check the link above for more info.

This test failed on {build.BranchName}.

CC {GetManagerMentions(repo)}";

                    //TODO: We'd like to link the test history here but TC api doens't make it easy
                    var tags = new List<string> { GitHubClientWrapper.TestFailureTag, GitHubUtils.GetBranchLabel(build.BranchName) };

                    Reporter.Output($"Creating new issue for test failure {failure.Name}...");
                    var issue = await GHClient.CreateIssue(owner, repo, subject, body, tags);
                    Reporter.Output($"Created issue {issue.HtmlUrl}");
                    Reporter.Output($"Adding new issue to project '{GHClient.Config.FlakyProjectColumn}'");
                    await GHClient.AddIssueToProject(issue, GHClient.Config.FlakyProjectColumn);
                    Reporter.Output($"Adding workflow comment to issue {issue.HtmlUrl}");
                    await GHClient.CreateComment(issue, WorkFlowComment);
                }
                // The issue already exists, comment on it if we haven't already done so for this build.
                else
                {
                    if (!testAggregates.TryGetValue(applicableIssues.First(), out var testNames))
                    {
                        testNames = new List<string>() { shortTestName };
                        testAggregates.Add(applicableIssues.First(), testNames);
                    }
                    else
                    {
                        testNames.Add(shortTestName);
                    }
                }
            }

            foreach (var test in testAggregates)
            {
                Reporter.Output($"Adding test failure comment to issue {test.Key.HtmlUrl}");
                await GHClient.CommentOnTest(build, test.Key, test.Value);
            }
        }

        private string GetManagerMentions(string repoName)
        {
            var repo = Config.GitHub.Repos.FirstOrDefault(r => r.Name.Equals(repoName, StringComparison.OrdinalIgnoreCase));

            if (repo == null || string.IsNullOrEmpty(repo.Manager))
            {
                // Default to Eilon
                return "@Eilon (because the bot doesn't know who else to pick)";
            }
            else
            {
                return GitHubUtils.GetAtMentions(repo.Manager);
            }
        }

        private static string GetTestName(TestOccurrence testOccurrence)
        {
            var shortTestName = testOccurrence.Name.Replace(Constants.VSTestPrefix, string.Empty);
            shortTestName = shortTestName.Split('(').First();
            return shortTestName.Split('.').Last();
        }

        private static Regex WordRegex = new Regex(@"[ \r\n\\?()]+", RegexOptions.Compiled);

        private static bool MessagesAreClose(string source, string target)
        {
            if (source == null && target == null)
            {
                return true;
            }
            else if (source == null || target == null)
            {
                return false;
            }

            var h1 = new HashSet<string>(WordRegex.Split(source).Distinct());
            var h2 = new HashSet<string>(WordRegex.Split(target).Distinct());

            var intersection = h1.Intersect(h2);
            var min = Math.Min(h1.Count, h2.Count);
            var percentSame = intersection.Count() / (double)min;

            // After a little testing and fiddling it seems that ~70% similarity of exception messages is a good heuristic for if things are "the same problem".
            // We expect this to cause the occasional false positive/negative, but let's see what they are before doing something more complicated here.
            return percentSame >= 0.7;
        }

        private static string GetExceptionFromIssue(Issue issue)
        {
            // We put exceptions inside of triple ticks on GitHub, split by that then figure out what was inside it.
            var parts = issue.Body.Split(new string[] { "```" }, StringSplitOptions.RemoveEmptyEntries);
            if (parts == null || parts.Length < 2)
            {
                return null;
            }
            else
            {
                var insideTicks = issue.Body.StartsWith("```", StringComparison.OrdinalIgnoreCase) ? parts[0] : parts[1];
                insideTicks = insideTicks.Trim();
                return SafeGetExceptionMessage(insideTicks);
            }
        }

        private IEnumerable<GitHubIssue> GetApplicableIssues(IEnumerable<GitHubIssue> issues, TestOccurrence failure)
        {
            Reporter.Output($"Finding applicable issues for failure '{failure.Name}'...");
            var testError = TCClient.GetTestFailureText(failure);
            var testException = SafeGetExceptionMessage(testError);
            var shortTestName = GetTestName(failure);

            foreach (var issue in issues)
            {
                Reporter.Output($"Considering issue {issue.HtmlUrl}...");
                var issueException = GetExceptionFromIssue(issue);

                // An issue is "applicable" if any of these are true:
                // 1. The issue has the test name in the subject.
                // 2. The issue exception message is the same as or close to the test exception message.
                if (issue.Title.Contains(shortTestName, StringComparison.OrdinalIgnoreCase)
                    || (issueException != null && issueException.Equals(testException))
                    || MessagesAreClose(issueException, testException))
                {
                    Reporter.Output($"\t^^^ This issue is applicable");
                    yield return issue;
                }
            }
        }
    }
}

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using GitHubProvider;
using TeamCityApi;

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

        public override async Task HandleFailure(TeamCityBuild build)
        {
            var tests = TCClient.GetTests(build);
            var failures = tests.Where(s => s.Status == BuildStatus.FAILURE);

            foreach (var failure in failures)
            {
                var repo = TestToRepoMapper.FindRepo(failure.Name, Reporter);

                var issuesTask = GHClient.GetFlakyIssues(repo: repo);
                
                var errors = TCClient.GetTestFailureText(failure);

                string exceptionMessage;
                if(string.IsNullOrEmpty(errors))
                {
                    exceptionMessage = NoStackTraceAvailable;
                }
                else
                {
                    exceptionMessage = ErrorParsing.GetExceptionMessage(errors);
                }

                var applicableIssues = GetApplicableIssues(await issuesTask, failure);

                var shortTestName = GetTestName(failure);
                if (applicableIssues.Count() == 0)
                {
                    var subject = $"Flaky test: {shortTestName}";
                    // TODO: CC area experts
                    var body = $@"This test [fails]({build.WebURL}) occasionally with the following error:
```
{errors}
```
";
                    //TODO: We'd like to link the test history here but TC api doens't make it easy
                    var tags = new List<string> { "Flaky" };

                    var issue = await GHClient.CreateIssue(repo, subject, body, tags);
                    await GHClient.AddIssueToProject(issue, new GitHubProjectColumn { Id = GHClient.Config.FlakyProjectColumn });
                }
                // The issue already exists, comment on it if we haven't already done so for this build.
                else
                {
                    var issue = applicableIssues.First();

                    var comments = await GHClient.GetIssueComments(issue);

                    var commentsAboutThisBuild = comments.Where(c => c.Body.Contains(build.WebURL.ToString()));

                    if(commentsAboutThisBuild.Count() == 0)
                    {
                        var comment = $"{shortTestName} [failed again]({build.WebURL}).";
                        await GHClient.CreateComment(applicableIssues.First(), comment);
                    }
                }
            }                
        }

        private static string GetTestName(TestOccurrence testOccurrence)
        {
            var shortTestName = testOccurrence.Name.Replace(Constants.VSTestPrefix, string.Empty);
            shortTestName = shortTestName.Split('(').First();
            return shortTestName.Split('.').Last();
        }

        private static int LevenshteinDistance(string source, string target)
        {
            // Use the Levenshtein distance for "fuzzy matching"
            var sourceLen = source == null ? 0 : source.Length;
            var targetLen = target == null ? 0 : target.Length;

            if(sourceLen == 0)
            {
                return 0;
            }

            if(targetLen == 0)
            {
                return 0;
            }

            var matrix = new int[sourceLen+1, targetLen+1];
            for (int i = 0; i <= sourceLen; i++) matrix[i, 0] = i;
            for (int j = 0; j <= targetLen; j++) matrix[0, j] = j;

            for(int i = 1; i <= sourceLen; i++)
            {
                var sourceChar = source[i-1];
                
                for(int j = 1;j <= targetLen; j++)
                {
                    var targetChar = target[j-1];
                    var cost = 0;
                    if(sourceChar != targetChar)
                    {
                        cost = 1;
                    }

                    matrix[i, j] = new int[] { matrix[i - 1, j] + 1, matrix[i, j - 1] + 1, matrix[i - 1, j - 1] + cost }.Min();
                }
            }

            return matrix[sourceLen, targetLen];
        }

        private static bool LevenstienClose(string source, string target)
        {
            if (source == null && target == null)
            {
                return true;
            }
            else if (source == null || target == null)
            {
                return false;
            }

            var dist = LevenshteinDistance(source, target);

            var percentSame = (source.Length - dist) / (double)source.Length;

            // After a little testing and fiddling it seems that ~70% similarity of exception messages is a good heuristic for if things are "the same problem".
            // We expect this to cause the occasional false positive/negative, but let's see what they are before doing something more complicated here.
            return percentSame >= 0.7;
        }

        private static string GetExceptionFromIssue(GithubIssue issue)
        {
            // We put exceptions inside of triple ticks on GitHub, split by that then figure out what was inside it.
            var parts = issue.Body.Split(new string[] { "```" }, StringSplitOptions.RemoveEmptyEntries);
            if (parts == null || parts.Length < 2)
            {
                return null;
            }
            else
            {
                string insideTicks;
                if (issue.Body.StartsWith("```"))
                {
                    insideTicks = parts[0];
                }
                else
                {
                    insideTicks = parts[1];
                }

                insideTicks = insideTicks.Trim();
                return ErrorParsing.GetExceptionMessage(insideTicks);
            }
        }

        private IEnumerable<GithubIssue> GetApplicableIssues(IEnumerable<GithubIssue> issues, TestOccurrence failure)
        {
            var testError = TCClient.GetTestFailureText(failure);
            var testException = ErrorParsing.GetExceptionMessage(testError);
            var shortTestName = GetTestName(failure);

            var applicableIssues = new List<GithubIssue>();

            foreach (var issue in issues)
            {
                var issueException = GetExceptionFromIssue(issue);

                // An issue is "applicable" if any of these are true:
                // 1. The issue has the test name in the subject.
                // 2. The issue exception message is the same as or close to the test exception message.
                if (issue.Title.Contains(shortTestName, StringComparison.InvariantCultureIgnoreCase)
                    || (issueException != null && issueException.Equals(testException))
                    || LevenstienClose(issueException, testException))
                {
                    applicableIssues.Add(issue);
                }
            }

            return applicableIssues;
        }
    }
}

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Common;
using Octokit;
using TriageBuildFailures.Abstractions;
using TriageBuildFailures.GitHub;

namespace TriageBuildFailures.Handlers
{
    /// <summary>
    /// When tests fail file an issue about it. If an issue has already been filed comment on the issue so people know it's still happening.
    /// </summary>
    public class HandleTestFailures : HandleFailureBase
    {
        private const string NoStackTraceAvailable = "No stacktrace available";

        public async override Task<bool> CanHandleFailure(ICIBuild build)
        {
            var tests = await GetClient(build).GetTests(build, BuildStatus.FAILURE);
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

        public override async Task HandleFailure(ICIBuild build)
        {
            var client = GetClient(build);
            var tests = await client.GetTests(build, BuildStatus.FAILURE);
            if (tests.Any(s => s.Status != BuildStatus.FAILURE))
            {
                throw new Exception("Tests which didn't fail got through somehow.");
            }

            var testAggregates = new Dictionary<GitHubIssue, List<string>>();

            foreach (var failure in tests)
            {
                Reporter.Output($"Inspecting test failure {failure.Name}...");
                var failureArea = TestToAreaMapper.FindTestProductArea(failure.Name, Reporter);
                var owner = "aspnet";
                var repo = "AspNetCore-Internal";

                var flakyIssues = await GHClient.GetFlakyIssues(owner, repo);

                var errors = await client.GetTestFailureText(failure);

                var applicableIssue = await GetApplicableIssue(client, flakyIssues, failure);

                var shortTestName = GetTestName(failure);
                if (applicableIssue == null)
                {
                    var subject = $"Test failure: {shortTestName}";
                    // TODO: CC area experts
                    var body = $@"This test [failed]({build.WebURL}) with the following error:

```
{TrimTestFailureText(errors)}
```

Other tests within that build may have failed with a similar message, but they are not listed here. Check the link above for more info.

This test failed on {build.Branch}.

CC {GetOwnerMentions(failureArea)}";

                    //TODO: We'd like to link the test history here but TC api doens't make it easy
                    var issueLabels = new List<string>
                    {
                        GitHubClientWrapper.TestFailureTag,
                        GitHubUtils.GetBranchLabel(build.Branch),
                    };
                    if (!string.IsNullOrEmpty(failureArea))
                    {
                        issueLabels.Add(failureArea);
                    }

                    var assignees = GetOwnerNames(failureArea);

                    var hiddenData = new List<KeyValuePair<string, object>>
                    {
                        new KeyValuePair<string, object>("_Type", build.GetType().FullName),
                        new KeyValuePair<string, object>("Id", build.Id),
                        new KeyValuePair<string, object>("CIType", build.CIType.FullName),
                        new KeyValuePair<string, object>("BuildTypeID", build.BuildTypeID),
                        new KeyValuePair<string, object>("BuildName", build.BuildName),
                        new KeyValuePair<string, object>("Status", build.Status),
                        new KeyValuePair<string, object>("Branch", build.Branch),
                        new KeyValuePair<string, object>("StartDate", build.StartDate),
                        new KeyValuePair<string, object>("WebURL", build.WebURL),
                        new KeyValuePair<string, object>("Failure:Status", failure.Status),
                        new KeyValuePair<string, object>("Failure:Name", failure.Name),
                        new KeyValuePair<string, object>("Failure:BuildId", failure.BuildId),
                        new KeyValuePair<string, object>("Failure:TestId", failure.TestId),
                    };

                    Reporter.Output($"Creating new issue for test failure {failure.Name}...");
                    var issue = await GHClient.CreateIssue(owner, repo, subject, body, issueLabels, assignees, hiddenData: hiddenData);
                    Reporter.Output($"Created issue {issue.HtmlUrl}");
                }
                // The issue already exists, comment on it if we haven't already done so for this build.
                else
                {
                    if (!testAggregates.TryGetValue(applicableIssue, out var testNames))
                    {
                        testNames = new List<string>() { shortTestName };
                        testAggregates.Add(applicableIssue, testNames);
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


        private string[] GetOwnerNames(string areaName)
        {
            var owners = Config.GitHub.IssueAreas
                .FirstOrDefault(area => area.AreaName.Equals(areaName, StringComparison.OrdinalIgnoreCase))
                ?.OwnerNames;

            if (string.IsNullOrEmpty(owners))
            {
                // Default to Eilon
                return new[] { "Eilon" };
            }
            else
            {
                return owners.Split(',').Select(owner => owner.Trim()).ToArray();
            }
        }

        private string GetOwnerMentions(string areaName)
        {
            return GitHubUtils.GetAtMentions(GetOwnerNames(areaName));
        }

        private static string GetTestName(ICITestOccurrence testOccurrence)
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

        private async Task<GitHubIssue> GetApplicableIssue(ICIClient client, IEnumerable<GitHubIssue> issues, ICITestOccurrence failure)
        {
            var testError = await client.GetTestFailureText(failure);
            var testException = SafeGetExceptionMessage(testError); ;
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
                    return issue;
                }
            }

            return null;
        }
    }
}

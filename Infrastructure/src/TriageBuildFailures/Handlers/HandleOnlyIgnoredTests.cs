// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using TriageBuildFailures.Abstractions;
using TriageBuildFailures.Handlers;

namespace TriageBuildFailures.Commands
{
    public class HandleOnlyIgnoredTests : HandleFailureBase
    {
        public override async Task<bool> CanHandleFailure(ICIBuild build)
        {
            if (build.Status == BuildStatus.PARTIALSUCCESS)
            {
                var tests = await GetClient(build).GetTestsAsync(build, BuildStatus.FAILURE);
                if(tests.Count() > 0 && tests.All(t => HandleTestFailures.IgnoredTests.Contains(HandleTestFailures.GetTestName(t))))
                {
                    return true;
                }
            }

            return false;
        }

        public override Task<IFailureHandlerResult> HandleFailure(ICIBuild build)
        {
            Reporter.Output($"{build.WebURL} only contained tests which are actively ignored. We'll do nothing with it.");
            return Task.FromResult<IFailureHandlerResult>(
                new FailureHandlerResult(build, applicableIssues: Array.Empty<ICIIssue>()));
        }
    }
}

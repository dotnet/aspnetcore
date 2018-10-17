// Copyright(c) .NET Foundation.All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using TriageBuildFailures.Abstractions;

namespace TriageBuildFailures.VSTS.Models
{
    internal class VSTSTestOccurrence : ICITestOccurrence
    {
        public VSTSTestCaseResult TestCaseResult;

        public VSTSTestOccurrence(VSTSTestCaseResult result)
        {
            TestCaseResult = result;
        }

        public Abstractions.BuildStatus Status {
            get {
                switch (TestCaseResult.Outcome)
                {
                    case "Passed":
                        return Abstractions.BuildStatus.SUCCESS;
                    case "NotExecuted":
                        return Abstractions.BuildStatus.UNKNOWN;
                    case "Failed":
                        return Abstractions.BuildStatus.FAILURE;
                    default:
                        throw new NotImplementedException(TestCaseResult.Outcome);
                }
            }
        }

        public string Name => TestCaseResult.AutomatedTestName;

        public string BuildId => TestCaseResult.Build.Id;

        public string TestId => TestCaseResult.TestCaseReferenceId.ToString();
    }
}
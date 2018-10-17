// Copyright(c) .NET Foundation.All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace TriageBuildFailures.VSTS.Models
{
    public class VSTSTestCaseResult
    {
        public int Id { get; set; }
        public VSTSProject Project { get; set; }
        public DateTime StartedDate { get; set; }
        public DateTime CompletedDate { get; set; }
        public double DurationInMs { get; set; }
        public string Outcome { get; set; }
        public int Revision { get; set; }
        public string State { get; set; }
        public VSTSTestCase TestCase { get; set; }
        public VSTSTestRun TestRun { get; set; }
        public DateTime LastUpdatedDate { get; set; }
        public int Priority { get; set; }
        public string ComputerName { get; set; }
        public Build Build { get; set; }
        public string ErrorMessage { get; set; }
        public DateTime CreatedDate { get; set; }
        public Uri Url { get; set; }
        public string FailureType { get; set; }
        public string AutomatedTestStorage { get; set; }
        public string AutomatedTestType { get; set; }
        public Area Area { get; set; }
        public string TestCaseTitle { get; set; }
        public string StackTrace { get; set; }
        public int TestCaseReferenceId { get; set; }
        public string AutomatedTestName { get; set; }
    }
}
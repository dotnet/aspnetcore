// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using TriageBuildFailures.Abstractions;

namespace TriageBuildFailures.Handlers
{
    public class FailureHandlerResult : IFailureHandlerResult
    {
        public FailureHandlerResult(ICIBuild build, IEnumerable<ICIIssue> applicableIssues)
        {
            if (build == null)
            {
                throw new ArgumentNullException(nameof(build));
            }

            if (applicableIssues == null)
            {
                throw new ArgumentNullException(nameof(applicableIssues));
            }

            Build = build;
            ApplicableIssues = applicableIssues;
        }

        public ICIBuild Build { get; }

        public IEnumerable<ICIIssue> ApplicableIssues { get; }
    }
}

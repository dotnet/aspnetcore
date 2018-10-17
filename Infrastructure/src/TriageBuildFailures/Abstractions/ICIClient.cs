// Copyright(c) .NET Foundation.All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TriageBuildFailures.Abstractions
{
    public interface ICIClient
    {
        Task<IEnumerable<ICIBuild>> GetFailedBuilds(DateTime startDate);

        Task<IEnumerable<string>> GetTags(ICIBuild build);

        Task SetTag(ICIBuild build, string tag);

        Task<string> GetBuildLog(ICIBuild build);

        Task<IEnumerable<ICITestOccurrence>> GetTests(ICIBuild build, BuildStatus? buildStatus = null);

        Task<string> GetTestFailureText(ICITestOccurrence failure);
    }
}

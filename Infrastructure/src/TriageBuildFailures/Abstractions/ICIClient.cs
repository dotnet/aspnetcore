// Copyright(c) .NET Foundation.All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TriageBuildFailures.Abstractions
{
    public interface ICIClient
    {
        Task<IEnumerable<ICIBuild>> GetFailedBuildsAsync(DateTime startDate);

        Task<IEnumerable<string>> GetTagsAsync(ICIBuild build);

        Task SetTagAsync(ICIBuild build, string tag);

        Task<string> GetBuildLogAsync(ICIBuild build);

        Task<IEnumerable<ICITestOccurrence>> GetTestsAsync(ICIBuild build, BuildStatus? buildStatus = null);

        Task<string> GetTestFailureTextAsync(ICITestOccurrence failure);

        Task ReportHandledAsync(IFailureHandlerResult result);
    }
}

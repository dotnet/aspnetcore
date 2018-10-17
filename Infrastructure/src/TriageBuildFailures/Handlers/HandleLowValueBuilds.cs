// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using TriageBuildFailures.Abstractions;

namespace TriageBuildFailures.Handlers
{
    /// <summary>
    /// Do nothing for a select list of projects which are low priority
    /// </summary>
    /// <remarks>
    /// Ideally over time these builds should go away.
    /// </remarks>
    public class HandleLowValueBuilds : HandleFailureBase
    {
        public override Task<bool> CanHandleFailure(ICIBuild build)
        {
            var config = build.GetCIConfig(Config);
            return Task.FromResult(config.BuildIdIgnoreList.Contains(build.BuildTypeID));
        }

        public override Task HandleFailure(ICIBuild build)
        {
            // Do nothing. We don't watch builds on these lists.
            return Task.CompletedTask;
        }
    }
}

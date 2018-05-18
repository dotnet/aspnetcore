// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using TriageBuildFailures.Email;
using TriageBuildFailures.GitHub;
using TriageBuildFailures.TeamCity;

namespace TriageBuildFailures.Handlers
{
    /// <summary>
    /// A base class for FailureHandlers.
    /// </summary>
    public abstract class HandleFailureBase : IFailureHandler
    {
        public Config Config { get; set; }
        public TeamCityClientWrapper TCClient { get; set; }
        public GitHubClientWrapper GHClient { get; set; }
        public EmailClient EmailClient { get; set; }
        public IReporter Reporter { get; set; }

        public abstract bool CanHandleFailure(TeamCityBuild build);
        public abstract Task HandleFailure(TeamCityBuild build);
    }
}

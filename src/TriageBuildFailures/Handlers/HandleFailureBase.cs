// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using EmailProvider;
using GitHubProvider;
using McMaster.Extensions.CommandLineUtils;
using TeamCityApi;

namespace TriageBuildFailures.Handlers
{
    /// <summary>
    /// A base class for FailureHandlers.
    /// </summary>
    public abstract class HandleFailureBase : IFailureHandler
    {
        public TeamCityClient TCClient { get; set; }
        public GitHubClient GHClient { get; set; }
        public EmailClient EmailClient { get; set; }
        public IReporter Reporter { get; set; }

        public abstract bool CanHandleFailure(TeamCityBuild build);
        public abstract Task HandleFailure(TeamCityBuild build);
    }
}

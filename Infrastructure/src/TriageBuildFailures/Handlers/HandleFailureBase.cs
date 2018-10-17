// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using TriageBuildFailures.Abstractions;
using TriageBuildFailures.Email;
using TriageBuildFailures.GitHub;

namespace TriageBuildFailures.Handlers
{
    /// <summary>
    /// A base class for FailureHandlers.
    /// </summary>
    public abstract class HandleFailureBase : IFailureHandler
    {
        public Config Config { get; set; }
        public IDictionary<Type, ICIClient> CIClients { get; set; }
        public GitHubClientWrapper GHClient { get; set; }
        public EmailClient EmailClient { get; set; }
        public IReporter Reporter { get; set; }

        public abstract Task<bool> CanHandleFailure(ICIBuild build);
        public abstract Task HandleFailure(ICIBuild build);

        protected ICIClient GetClient(ICIBuild build)
        {
            return CIClients[build.CIType];
        }
    }
}

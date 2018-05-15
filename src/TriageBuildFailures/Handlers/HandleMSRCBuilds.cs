﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using TriageBuildFailures.TeamCity;

namespace TriageBuildFailures.Handlers
{
    /// <summary>
    /// Never ever post anything about any config with MSRC in the name publicly. Notify the build buddy, who will likely forward to the engineering alias.
    /// </summary>
    public class HandleMSRCBuilds : HandleFailureBase
    {
        public override bool CanHandleFailure(TeamCityBuild build)
        {
            // TODO: Have an allowlist of projects which don't contain MSRC configs.
            // If anything NOT on that allowlist fails, this FailureHandler should send it straight to my inbox.
            return build.BuildTypeID.Contains("MSRC", StringComparison.InvariantCultureIgnoreCase);
        }

        public override async Task HandleFailure(TeamCityBuild build)
        {
            var subject = $"Failure of MSRC {build.BuildName}";
            var body = $"{build.WebURL} failed, we don't want to do anything automatic to it because the name says MSRC.";

            // We don't want to create issues or anything public about MSRC builds, send an email to BBFL and he'll deal with it
            await EmailClient.SendEmail(EmailClient.Config.BuildBuddyEmail, subject, body);
        }
    }
}

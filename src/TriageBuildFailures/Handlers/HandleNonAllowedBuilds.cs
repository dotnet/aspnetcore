// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using TriageBuildFailures.Abstractions;

namespace TriageBuildFailures.Handlers
{
    /// <summary>
    /// Never ever post anything about any config we don't explicitly allow. Notify the build buddy, who will likely forward to the engineering alias.
    /// </summary>
    public class HandleNonAllowedBuilds : HandleFailureBase
    {
        public override Task<bool> CanHandleFailure(ICIBuild build)
        {
            var config = build.GetCIConfig(Config);
            return Task.FromResult(!config.BuildIdAllowList.Contains(build.BuildTypeID));
        }

        public override async Task HandleFailure(ICIBuild build)
        {
            var subject = $"Failure of unknown build '{build.BuildTypeID}'";
            var body = $"{build.WebURL} failed, we don't want to do anything automatic to it because it's not on the allow-list.";

            // We don't want to create issues or anything public about MSRC builds, send an email to BBFL and he'll deal with it
            await EmailClient.SendEmail(EmailClient.Config.BuildBuddyEmail, subject, body);
        }
    }
}

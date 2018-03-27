// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Dotnet.Scripts
{
    public class Config
    {
        public string UpdatedVersions { get; set; }
        public string BuildXml { get; set; }
        public string GithubUsername {get; set;}
        public string GithubEmail {get; set;}
        public string GithubToken {get; set;}
        public string GithubUpstreamOwner {get; set;} = "aspnet";
        public string GithubProject {get; set;} = "Universe";
        public string GithubUpstreamBranch {get; set;} = "dev";
        public string[] GitHubPullRequestNotifications { get; set; } = new string[] { };

        public string[] UpdatedVersionsList
        {
            get
            {
                return UpdatedVersions.Split('+',System.StringSplitOptions.RemoveEmptyEntries);
            }
        }
    }
}

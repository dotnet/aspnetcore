// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace TriageBuildFailures
{
    public class Config
    {
        public string GitHubAccessToken { get; set; }

        public string TeamCityServer {
            get {
                return "aspnetci";
            }
        }

        public string TeamCityUser {
            get {
                return "redmond\\asplab";
            }
        }

        public string TeamCityPassword { get; set; }
    }
}

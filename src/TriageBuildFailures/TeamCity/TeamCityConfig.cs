// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


using System.Collections.Generic;

namespace TriageBuildFailures.TeamCity
{
    public class TeamCityConfig
    {
        public IEnumerable<string> BuildIdAllowList { get; set; }

        public string Server { get; set; }

        public string User { get; set; }

        public string Password { get; set; }
    }
}

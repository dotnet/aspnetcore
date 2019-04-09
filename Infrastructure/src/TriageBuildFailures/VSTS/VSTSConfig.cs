// Copyright(c) .NET Foundation.All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using TriageBuildFailures.Abstractions;

namespace TriageBuildFailures.VSTS
{
    public class VSTSConfig : CIConfigBase
    {
        public string Account { get; set; }

        public string BuildPath { get; set; }

        public string PersonalAccessToken { get; set; }

        public IEnumerable<ReleaseDefinition> ReleaseIdIgnoreList { get; set; }
    }

    public class ReleaseDefinition
    {
        public string Id { get; set; }
        public string Project { get; set; }
    }
}

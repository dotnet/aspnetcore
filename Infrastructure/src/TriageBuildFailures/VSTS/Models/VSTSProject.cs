// Copyright(c) .NET Foundation.All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


namespace TriageBuildFailures.VSTS.Models
{
    public class VSTSProject
    {
        public string Abbreviation { get; set; }
        public string Description { get; set; }
        public string Id { get; set; }
        public string LastUpdateTime { get; set; }
        public string Name { get; set; }
        public string Uri { get; set; }
    }
}

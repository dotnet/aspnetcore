// Copyright(c) .NET Foundation.All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace TriageBuildFailures.VSTS.Models
{
    public partial class ThinRelease
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public ReleaseStatus Status { get; set; }
        public ReleaseDefinition ReleaseDefinition { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime ModifiedOn { get; set; }
        public Links _Links { get; set; }
        public VSTSProject ProjectReference { get; set; }
    }
}

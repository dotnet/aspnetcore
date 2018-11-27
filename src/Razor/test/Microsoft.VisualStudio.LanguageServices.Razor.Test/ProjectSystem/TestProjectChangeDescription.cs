// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information

using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.VisualStudio.ProjectSystem
{
    internal class TestProjectChangeDescription : IProjectChangeDescription
    {
        public string RuleName { get; set; }

        public TestProjectRuleSnapshot Before { get; set; }

        public IProjectChangeDiff Difference { get; set; }

        public TestProjectRuleSnapshot After { get; set; }

        IProjectRuleSnapshot IProjectChangeDescription.Before => Before;

        IProjectChangeDiff IProjectChangeDescription.Difference => Difference;

        IProjectRuleSnapshot IProjectChangeDescription.After => After;
    }
}
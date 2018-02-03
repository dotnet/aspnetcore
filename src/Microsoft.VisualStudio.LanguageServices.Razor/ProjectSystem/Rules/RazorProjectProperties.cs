// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem.Rules
{
    [Export]
    internal partial class RazorProjectProperties : StronglyTypedPropertyAccess
    {
        [ImportingConstructor]
        public RazorProjectProperties(ConfiguredProject configuredProject) 
            : base(configuredProject)
        {
        }

        public RazorProjectProperties(ConfiguredProject configuredProject, UnconfiguredProject unconfiguredProject)
            : base(configuredProject, unconfiguredProject)
        {
        }

        public RazorProjectProperties(ConfiguredProject configuredProject, IProjectPropertiesContext projectPropertiesContext) 
            : base(configuredProject, projectPropertiesContext)
        {
        }

        public RazorProjectProperties(ConfiguredProject configuredProject, string file, string itemType, string itemName) 
            : base(configuredProject, file, itemType, itemName)
        {
        }
    }
}

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    internal class TestProjectSnapshotProjectEngineFactory : ProjectSnapshotProjectEngineFactory
    {
        public RazorProjectEngine Engine { get; set; }

        public override RazorProjectEngine Create(ProjectSnapshot project, RazorProjectFileSystem fileSystem, Action<RazorProjectEngineBuilder> configure)
        {
            return Engine ?? RazorProjectEngine.Create(project.Configuration, fileSystem, configure);
        }

        public override IProjectEngineFactory FindFactory(ProjectSnapshot project)
        {
            throw new NotImplementedException();
        }

        public override IProjectEngineFactory FindSerializableFactory(ProjectSnapshot project)
        {
            throw new NotImplementedException();
        }
    }
}

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    internal class DefaultDocumentSnapshot : DocumentSnapshot
    {
        public DefaultDocumentSnapshot(DefaultProjectSnapshot project, DocumentState state)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            ProjectInternal = project;
            State = state;
        }

        public DefaultProjectSnapshot ProjectInternal { get; }

        public DocumentState State { get; }

        public override string FilePath => State.HostDocument.FilePath;

        public override string TargetPath => State.HostDocument.TargetPath;

        public override ProjectSnapshot Project => ProjectInternal;

        public override IReadOnlyList<DocumentSnapshot> GetImports()
        {
            return State.Imports.GetImports(Project, this);
        }

        public override Task<SourceText> GetTextAsync()
        {
            return State.GetTextAsync();
        }

        public override Task<VersionStamp> GetTextVersionAsync()
        {
            return State.GetTextVersionAsync();
        }

        public override Task<RazorCodeDocument> GetGeneratedOutputAsync()
        {
            // IMPORTANT: Don't put more code here. We want this to return a cached task.
            return State.GeneratedOutput.GetGeneratedOutputInitializationTask(Project, this);
        }

        public override bool TryGetText(out SourceText result)
        {
            return State.TryGetText(out result);
        }

        public override bool TryGetTextVersion(out VersionStamp result)
        {
            return State.TryGetTextVersion(out result);
        }

        public override bool TryGetGeneratedOutput(out RazorCodeDocument result)
        {
            if (State.GeneratedOutput.IsResultAvailable)
            {
                result = State.GeneratedOutput.GetGeneratedOutputInitializationTask(Project, this).Result;
                return true;
            }

            result = null;
            return false;
        }
    }
}
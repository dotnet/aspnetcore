// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

// Temporary code until we get access to these APIs
#if WORKSPACE_PROJECT_CONTEXT_FACTORY

using System;
using Microsoft.VisualStudio.LanguageServices.Implementation.TaskList;

namespace Microsoft.VisualStudio.LanguageServices.ProjectSystem
{
    // The 2 is needed here to prevent clashes with the real interface. MEF is based on the FullName
    // as a string.
    internal interface IWorkspaceProjectContextFactory2
    {
        IWorkspaceProjectContext CreateProjectContext(string languageName, string projectDisplayName, string projectFilePath, Guid projectGuid, object hierarchy, string binOutputPath);


        IWorkspaceProjectContext CreateProjectContext(string languageName, string projectDisplayName, string projectFilePath, Guid projectGuid, object hierarchy, string binOutputPath, ProjectExternalErrorReporter errorReporter);
    }
}

#endif
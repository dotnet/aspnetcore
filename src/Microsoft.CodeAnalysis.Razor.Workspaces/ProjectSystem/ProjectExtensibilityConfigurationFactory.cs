// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    internal abstract class ProjectExtensibilityConfigurationFactory : ILanguageService
    {
        public abstract Task<ProjectExtensibilityConfiguration> GetConfigurationAsync(Project project, CancellationToken cancellationToken = default(CancellationToken));
    }
}

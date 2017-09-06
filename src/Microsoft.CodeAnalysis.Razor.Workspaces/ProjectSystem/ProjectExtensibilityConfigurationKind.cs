// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    /// <summary>
    /// Describes how closely the configuration of Razor tooling matches the actual project dependencies.
    /// </summary>
    internal enum ProjectExtensibilityConfigurationKind
    {
        ApproximateMatch,
        Fallback,
    }
}

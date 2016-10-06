// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Extensions.ProjectModel.Resolution
{
    public enum DependencyType
    {
        Target,
        Package,
        Assembly,
        Project,
        AnalyzerAssembly,
        Unknown
    }
}

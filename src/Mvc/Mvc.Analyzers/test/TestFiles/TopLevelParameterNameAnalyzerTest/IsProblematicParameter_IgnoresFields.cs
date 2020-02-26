// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.Analyzers.TopLevelParameterNameAnalyzerTestFiles
{
    public class IsProblematicParameter_IgnoresFields
    {
        public string model;

        public void ActionMethod(IsProblematicParameter_IgnoresFields model) { }
    }
}

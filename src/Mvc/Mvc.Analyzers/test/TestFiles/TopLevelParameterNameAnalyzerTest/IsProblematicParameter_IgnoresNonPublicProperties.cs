// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.Analyzers.TopLevelParameterNameAnalyzerTestFiles
{
    public class IsProblematicParameter_IgnoresNonPublicProperties
    {
        protected string Model { get; set; }

        public void ActionMethod(IsProblematicParameter_IgnoresNonPublicProperties model) { }
    }
}

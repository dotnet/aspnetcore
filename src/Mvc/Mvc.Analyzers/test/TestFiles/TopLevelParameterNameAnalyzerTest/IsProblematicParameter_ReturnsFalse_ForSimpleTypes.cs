// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Mvc.Analyzers.TopLevelParameterNameAnalyzerTestFiles
{
    public class IsProblematicParameter_ReturnsFalse_ForSimpleTypes
    {
        public void ActionMethod(DateTime date, DateTime? day, Uri absoluteUri, Version majorRevision, DayOfWeek sunday) { }
    }
}

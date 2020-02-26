// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Mvc.Analyzers.TopLevelParameterNameAnalyzerTestFiles
{
    public class IsProblematicParameter_ReturnsFalse_ForSimpleTypes
    {
        public void ActionMethod(DateTime date, DateTime? day, Uri absoluteUri, Version majorRevision, DayOfWeek sunday) { }
    }
}

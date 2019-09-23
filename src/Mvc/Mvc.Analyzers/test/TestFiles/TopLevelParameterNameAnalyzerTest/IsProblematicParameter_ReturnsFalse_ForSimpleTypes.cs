using System;

namespace Microsoft.AspNetCore.Mvc.Analyzers.TopLevelParameterNameAnalyzerTestFiles
{
    public class IsProblematicParameter_ReturnsFalse_ForSimpleTypes
    {
        public void ActionMethod(DateTime date, DateTime? day, Uri absoluteUri, Version majorRevision, DayOfWeek sunday) { }
    }
}

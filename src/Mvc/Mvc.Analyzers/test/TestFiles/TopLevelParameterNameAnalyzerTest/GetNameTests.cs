// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.Analyzers.TopLevelParameterNameAnalyzerTestFiles
{
    public class GetNameTests
    {
        public void NoAttribute(int param) { }

        public void SingleAttribute([ModelBinder(Name = "testModelName")] int param) { }

        public void SingleAttributeWithoutName([ModelBinder] int param) { }

        public void MultipleAttributes([ModelBinder(Name = "name1")][Bind(Prefix = "name2")] int param) { }
    }
}

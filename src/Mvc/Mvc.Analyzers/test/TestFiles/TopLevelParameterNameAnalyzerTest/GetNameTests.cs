// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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

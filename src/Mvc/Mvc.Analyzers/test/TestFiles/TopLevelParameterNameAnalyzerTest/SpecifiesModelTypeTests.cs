// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

namespace Microsoft.AspNetCore.Mvc.Analyzers.TopLevelParameterNameAnalyzerTestFiles
{
    public class SpecifiesModelTypeTests
    {
        public void SpecifiesModelType_ReturnsFalse_IfModelBinderDoesNotSpecifyType(
            [ModelBinder(Name = "Name")] object model) { }

        public void SpecifiesModelType_ReturnsTrue_IfModelBinderSpecifiesTypeFromConstructor(
            [ModelBinder(typeof(SimpleTypeModelBinder))] object model) { }

        public void SpecifiesModelType_ReturnsTrue_IfModelBinderSpecifiesTypeFromProperty(
            [ModelBinder(BinderType = typeof(SimpleTypeModelBinder))] object model) { }
    }
}

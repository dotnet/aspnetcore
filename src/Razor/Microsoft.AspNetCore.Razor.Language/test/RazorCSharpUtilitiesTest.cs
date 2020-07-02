// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Razor.Language
{
    public class RazorCSharpUtilitiesTest
    {
        [Theory]
        [InlineData("", false, "", "")]
        [InlineData(".", true, "", "")]
        [InlineData("Foo", true, "", "Foo")]
        [InlineData("SomeProject.Foo", true, "SomeProject", "Foo")]
        [InlineData("SomeProject.Foo<Bar>", true, "SomeProject", "Foo<Bar>")]
        [InlineData("SomeProject.Foo<Bar.Baz>", true, "SomeProject", "Foo<Bar.Baz>")]
        [InlineData("SomeProject.Foo<Bar.Baz>>", true, "", "SomeProject.Foo<Bar.Baz>>")]
        [InlineData("SomeProject..Foo<Bar>", true, "SomeProject.", "Foo<Bar>")]
        public void TrySplitNamespaceAndType_WorksAsExpected(string fullTypeName, bool expectedResult, string expectedNamespace, string expectedTypeName)
        {
            // Arrange & Act
            var result = RazorCSharpUtilities.TrySplitNamespaceAndType(fullTypeName, out var @namespace, out var typeName);

            // Assert
            Assert.Equal(expectedResult, result);
            Assert.Equal(expectedNamespace, DefaultRazorTagHelperBinderPhase.ComponentDirectiveVisitor.GetTextSpanContent(@namespace, fullTypeName));
            Assert.Equal(expectedTypeName, DefaultRazorTagHelperBinderPhase.ComponentDirectiveVisitor.GetTextSpanContent(typeName, fullTypeName));
        }
    }
}

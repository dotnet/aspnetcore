// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Microsoft.AspNetCore.Components.Razor
{
    public class GlobalQualifiedTypeNameRewriterTest
    {
        [Theory]
        [InlineData("String", "global::String")]
        [InlineData("System.String", "global::System.String")]
        [InlineData("TItem2", "TItem2")]
        [InlineData("System.Collections.Generic.List<System.String>", "global::System.Collections.Generic.List<global::System.String>")]
        [InlineData("System.Collections.Generic.Dictionary<System.String, TItem1>", "global::System.Collections.Generic.Dictionary<global::System.String, TItem1>")]
        [InlineData("System.Collections.TItem3.Dictionary<System.String, TItem1>", "global::System.Collections.TItem3.Dictionary<global::System.String, TItem1>")]
        [InlineData("System.Collections.TItem3.TItem1<System.String, TItem1>", "global::System.Collections.TItem3.TItem1<global::System.String, TItem1>")]

        // This case is interesting because we know TITem2 to be a generic type parameter,
        // and we know that this will never be valid, which is why we don't bother rewriting.
        [InlineData("TItem2<System.String, TItem1>", "TItem2<global::System.String, TItem1>")]
        public void GlobalQualifiedTypeNameRewriter_CanQualifyNames(string original, string expected)
        {
            // Arrange
            var visitor = new GlobalQualifiedTypeNameRewriter(new[] { "TItem1", "TItem2", "TItem3" });

            var parsed = SyntaxFactory.ParseTypeName(original);

            // Act
            var actual = visitor.Visit(parsed);

            // Assert
            Assert.Equal(expected, actual.ToString());
        }
    }
}

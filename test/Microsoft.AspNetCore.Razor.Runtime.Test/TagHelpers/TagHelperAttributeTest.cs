// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Razor.TagHelpers
{
    public class TagHelperAttributeTest
    {
        public static TheoryData CopyConstructorData
        {
            get
            {
                return new TheoryData<IReadOnlyTagHelperAttribute>
                {
                    new TagHelperAttribute("hello", "world") { Minimized = false },
                    new TagHelperAttribute("checked", value: null) { Minimized = true },
                };
            }
        }

        [Theory]
        [MemberData(nameof(CopyConstructorData))]
        public void CopyConstructorCopiesValuesAsExpected(IReadOnlyTagHelperAttribute readOnlyTagHelperAttribute)
        {
            // Act
            var tagHelperAttribute = new TagHelperAttribute(readOnlyTagHelperAttribute);

            // Assert
            Assert.Equal(
                readOnlyTagHelperAttribute,
                tagHelperAttribute,
                CaseSensitiveTagHelperAttributeComparer.Default);
        }
    }
}

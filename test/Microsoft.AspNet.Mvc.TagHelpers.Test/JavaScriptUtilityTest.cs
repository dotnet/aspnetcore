// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.AspNet.Mvc.TagHelpers.Test
{
    public class JavaScriptUtilityTest
    {
        [Theory]
        [InlineData("Hello World", "Hello World")]
        [InlineData("Hello & World", "Hello \\u0026 World")]
        [InlineData("Hello \r World", "Hello \\r World")]
        [InlineData("Hello \n World", "Hello \\n World")]
        [InlineData("Hello < World", "Hello \\u003c World")]
        [InlineData("Hello > World", "Hello \\u003e World")]
        [InlineData("Hello ' World", "Hello \\u0027 World")]
        [InlineData("Hello \" World", "Hello \\u0022 World")]
        [InlineData("Hello \\ World", "Hello \\\\ World")]
        [InlineData("Hello \u0005 \u001f World", "Hello \\u0005 \\u001f World")]
        [InlineData("Hello \r\n <ah /> 'eep' & \"hey\" World", "Hello \\r\\n \\u003cah /\\u003e \\u0027eep\\u0027 \\u0026 \\u0022hey\\u0022 World")]
        public void JavaScriptEncode_EncodesCorrectly(string input, string expectedOutput)
        {
            // Act
            var result = JavaScriptUtility.JavaScriptStringEncode(input);

            // Assert
            Assert.Equal(expectedOutput, result);
        }

        [Theory]
        [InlineData("window.alert(\"[[[0]]]\")", "window.alert(\"{0}\")")]
        [InlineData("var test = { a: 1 };", "var test = {{ a: 1 }};")]
        [InlineData("var test = { a: 1, b: \"[[[0]]]\" };", "var test = {{ a: 1, b: \"{0}\" }};")]
        public void PrepareFormatString_PreparesJavaScriptCorrectly(string input, string expectedOutput)
        {
            // Act
            var result = JavaScriptUtility.PrepareFormatString(input);

            // Assert
            Assert.Equal(expectedOutput, result);
        }
    }
}
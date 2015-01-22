// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.AspNet.Mvc
{
    public class StringHelperTest
    {
        public static TheoryData TrimSpacesAndCharsData
        {
            get
            {
                // input, trimCharacters, expectedOutput
                return new TheoryData<string, char[], string>
                {
                    { "abcd", new char[] {  }, "abcd" },
                    { "  /.", new char[] { '/', '.' }, string.Empty },
                    { string.Empty, new char[] {  }, string.Empty },
                    { " ", new char[] {  }, string.Empty },
                    { "  ", new char[] {  }, string.Empty },
                    { "  / ", new char[] { '/' }, string.Empty },
                    { "  \t ", new char[] { '/' }, string.Empty },
                    { "   ", new char[] { '/' }, string.Empty },
                    { "  ", new char[] { '/' }, string.Empty },
                    { "/", new char[] { '/' }, string.Empty },
                    { "//", new char[] { '/' }, string.Empty },
                    { "//  ", new char[] { '/' }, string.Empty },
                    { "/ ", new char[] { '/' }, string.Empty },
                    { " a ", new char[] {  }, "a" },
                    { " a", new char[] {  }, "a" },
                    { "a ", new char[] {  }, "a" },
                    { "  a  ", new char[] {  }, "a" },
                    { " a \n\r", new char[] {  }, "a" },
                    { "\t\r a ", new char[] {  }, "a" },
                    { "\ta ", new char[] {  }, "a" },
                    { " a a ", new char[] {  }, "a a" },
                    { " a ", new char[] { '/' }, "a" },
                    { " a", new char[] { '/' }, "a" },
                    { "a ", new char[] { '/' }, "a" },
                    { "  a  ", new char[] { '/' }, "a" },
                    { " a \n\r", new char[] { '/' }, "a" },
                    { "\t\r a ", new char[] { '/' }, "a" },
                    { "\ta ", new char[] { '/' }, "a" },
                    { " a a ", new char[] { '/' }, "a a" },
                    { " a ", new char[] { '/', ' ' }, "a" },
                    { " a", new char[] { '/', ' ' }, "a" },
                    { "a ", new char[] { '/', ' ' }, "a" },
                    { "  a  ", new char[] { '/', ' ' }, "a" },
                    { " a \n\r", new char[] { '/', ' ' }, "a" },
                    { "\t\r a ", new char[] { '/', ' ' }, "a" },
                    { "\ta ", new char[] { '/', ' ' }, "a" },
                    { " a a ", new char[] { '/', ' ' }, "a a" },
                    { "/ a ", new char[] { '/' }, "a" },
                    { " / a", new char[] { '/' }, "a" },
                    { "a / /", new char[] { '/' }, "a" },
                    { "  a  // //", new char[] { '/' }, "a" },
                    { " a \n\r//", new char[] { '/' }, "a" },
                    { "////\t\r a ", new char[] { '/' }, "a" },
                    { "\ta /", new char[] { '/' }, "a" },
                    { " a/ a ", new char[] { '/' }, "a/ a" },
                    { "/ a ", new char[] { '/', ' ' }, "a" },
                    { " / a", new char[] { '/', ' ' }, "a" },
                    { "a / /", new char[] { '/', ' ' }, "a" },
                    { "  a  // //", new char[] { '/', ' ' }, "a" },
                    { " a \n\r//", new char[] { '/', ' ' }, "a" },
                    { "////\t\r a ", new char[] { '/', ' ' }, "a" },
                    { "\ta /", new char[] { '/', ' ' }, "a" },
                    { " a/ a ", new char[] { '/', ' ' }, "a/ a" },
                    { " a /.", new char[] { '/', '.' }, "a" },
                    { " a", new char[] { '/', '.' }, "a" },
                    { "/. ./a ", new char[] { '/', '.' }, "a" },
                    { "  a  ", new char[] { '/', '.' }, "a" },
                    { " a \n\r", new char[] { '/', '.' }, "a" },
                    { "\t\r a ", new char[] { '/', '.' }, "a" },
                    { "\ta ", new char[] { '/', '.' }, "a" },
                    { "///..a/./a /. ./....", new char[] { '/', '.' }, "a/./a" },
                };
            }
        }

        [Theory]
        [MemberData(nameof(TrimSpacesAndCharsData))]
        public void TrimSpacesAndChars_GeneratesExpectedOutput(
            string input,
            char[] trimCharacters,
            string expectedOutput)
        {
            // Arrange & Act
            var output = StringHelper.TrimSpacesAndChars(input, trimCharacters);

            // Assert
            Assert.Equal(expectedOutput, output, StringComparer.Ordinal);
        }
    }
}
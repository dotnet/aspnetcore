// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Razor.Language
{
    public class HtmlCaseTest
    {
        public static TheoryData HtmlConversionData
        {
            get
            {
                return new TheoryData<string, string>
                {
                    { "SomeThing", "some-thing" },
                    { "someOtherThing", "some-other-thing" },
                    { "capsONInside", "caps-on-inside" },
                    { "CAPSOnOUTSIDE", "caps-on-outside" },
                    { "ALLCAPS", "allcaps" },
                    { "One1Two2Three3", "one1-two2-three3" },
                    { "ONE1TWO2THREE3", "one1two2three3" },
                    { "First_Second_ThirdHi", "first_second_third-hi" }
                };
            }
        }

        [Theory]
        [MemberData(nameof(HtmlConversionData))]
        public void ToHtmlCase_ReturnsExpectedConversions(string input, string expectedOutput)
        {
            // Arrange, Act
            var output = HtmlCase.ToHtmlCase(input);

            // Assert
            Assert.Equal(output, expectedOutput);
        }
    }
}
